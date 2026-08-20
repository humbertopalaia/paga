# Design Document — mvp-5-infra-manual-deploy

## Overview

This document describes the architecture and implementation plan for provisioning the PAGA MVP infrastructure on AWS using CloudFormation, Nginx reverse proxy, CodeDeploy lifecycle scripts, production CORS hardening, and operational documentation (runbook + ADR).

All artifacts are declarative configuration (YAML templates, shell scripts, Nginx config) validated offline. No real AWS resources are created during implementation — only `aws cloudformation validate-template` is executed.

---

## Architecture

### Infrastructure Topology

```
Internet (HTTPS)
    │
    ▼
CloudFront Distribution (*.cloudfront.net)
    ├── Default Behavior → S3 Bucket (Angular SPA, via OAC)
    └── /api/* Behavior  → EC2 t3.micro (HTTP port 80)
                              │
                              ▼
                           Nginx :80
                              │
                              ├── /api/*  → proxy_pass http://localhost:5000
                              └── /health → proxy_pass http://localhost:5000
                              │
                              ▼
                           Kestrel :5000 (.NET 10 API)
                              │
                              ▼
                           PostgreSQL :5432 (localhost only)
```

### Stack Dependency Graph

```
┌───────────────┐     ┌───────────────┐
│ Network Stack │     │   IAM Stack   │
└───────┬───────┘     └───────┬───────┘
        │ exports:              │ exports:
        │ VPC ID                │ InstanceProfileArn
        │ SubnetId              │ CodeDeployRoleArn
        │ SecurityGroupId       │
        ▼                       ▼
┌───────────────────────────────────────┐
│            EC2 Stack                  │
│  (imports from Network + IAM)         │
└───────────────────┬───────────────────┘
                    │ output: PublicIp
                    ▼
          ┌───────────────────┐
          │  Frontend Stack   │
          │  (param: EC2 IP)  │
          └───────────────────┘
```

**Creation order:** Network → IAM → EC2 → Frontend

---

## Components

### 1. CloudFormation Templates (`infra/cloudformation/`)

#### 1.1 network.yaml

| Resource | Purpose |
|----------|---------|
| `AWS::EC2::VPC` | Isolated network; CIDR from parameter (default `10.0.0.0/16`) |
| `AWS::EC2::Subnet` | One public subnet (AZ `a` of current region) |
| `AWS::EC2::InternetGateway` + `VPCGatewayAttachment` | Outbound/inbound internet access |
| `AWS::EC2::RouteTable` + `Route` | Default route `0.0.0.0/0` → IGW |
| `AWS::EC2::SubnetRouteTableAssociation` | Bind subnet to route table |
| `AWS::EC2::SecurityGroup` | Ingress rules (see below) |

**Security Group Rules:**

| Port | Protocol | Source | Purpose |
|------|----------|--------|---------|
| 80 | TCP | `com.amazonaws.global.cloudfront.origin-facing` prefix list | HTTP from CloudFront |
| 443 | TCP | `com.amazonaws.global.cloudfront.origin-facing` prefix list | HTTPS from CloudFront |
| 22 | TCP | `SshCidr` parameter (required, no default) | Admin SSH |

Port 5432 is **never** opened — PostgreSQL listens only on localhost.

**Parameters:**
- `Environment` (String, default `prod`)
- `VpcCidr` (String, default `10.0.0.0/16`)
- `SshCidr` (String, **required** — no default)

**Exports:**
- `paga-{Environment}-vpc-id`
- `paga-{Environment}-subnet-id`
- `paga-{Environment}-sg-id`

#### 1.2 iam.yaml

Four IAM roles with least-privilege policies:

| Role | Key Permissions |
|------|----------------|
| CodePipelineRole | `codebuild:*`, `codedeploy:*`, S3 artifact bucket read/write |
| CodeBuildRole | S3 source/output artifact, CloudWatch Logs, `s3:PutObject` for frontend bucket, `cloudfront:CreateInvalidation` |
| CodeDeployRole | `codedeploy:*` for the app, S3 artifact read, EC2 tag read |
| EC2InstanceRole + InstanceProfile | SSM `GetParameter` on `/paga/*`, S3 artifact bucket read |

**Parameters:**
- `Environment` (String, default `prod`)
- `ArtifactBucketArn` (String, required)
- `FrontendBucketArn` (String, required)
- `CloudFrontDistributionArn` (String, required)

**Exports:**
- `paga-{Environment}-instance-profile-arn`
- `paga-{Environment}-codedeploy-role-arn`

#### 1.3 ec2.yaml

Single `AWS::EC2::Instance` with:
- Instance type from parameter (default `t3.micro`)
- AMI: Amazon Linux 2023 (latest, via SSM public parameter `resolve:ssm:/aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64`)
- Network: imported VPC subnet + security group
- IAM: imported instance profile
- `AssociatePublicIpAddress: true` (via network interface in the launch)
- User data (bash script, `#!/bin/bash -xe`)

**User Data Script Responsibilities:**
1. Install .NET 10 runtime (`dnf install dotnet-runtime-10.0`)
2. Install PostgreSQL 16 server + psql client
3. Install Nginx
4. Install CodeDeploy agent
5. Configure PostgreSQL: `listen_addresses = 'localhost'`, create database `paga`, set password from SSM (`/paga/db-password`)
6. Enable services on boot: `postgresql`, `nginx`, `codedeploy-agent`
7. Create app directory (`/opt/paga/api`)
8. Create systemd unit file for Kestrel (`paga-api.service`)

**Parameters:**
- `Environment` (String, default `prod`)
- `InstanceType` (String, default `t3.micro`)
- `KeyPairName` (String, required)

**Exports:**
- `paga-{Environment}-ec2-public-ip`

#### 1.4 frontend.yaml

| Resource | Purpose |
|----------|---------|
| `AWS::S3::Bucket` | SPA hosting, parameterized name |
| `AWS::S3::BucketPolicy` | Allow CloudFront OAC only |
| `AWS::CloudFront::OriginAccessControl` | S3 access via signed requests |
| `AWS::CloudFront::Distribution` | CDN with two origins |

**CloudFront Behaviors:**

| Pattern | Origin | Protocol | Cache |
|---------|--------|----------|-------|
| Default (`*`) | S3 (OAC) | HTTPS | CachingOptimized |
| `/api/*` | EC2 (custom HTTP :80) | HTTP only | CachingDisabled (all forwarded) |

**SPA Fallback:** `CustomErrorResponses` for 403 and 404 → `/index.html` with status 200.

No custom domain, no ACM certificate, no Aliases.

**Parameters:**
- `Environment` (String, default `prod`)
- `BucketName` (String, required)
- `Ec2PublicIp` (String, required — EC2 public IP for API origin)

**Exports:**
- `paga-{Environment}-cloudfront-domain`
- `paga-{Environment}-cloudfront-distribution-id`

---

### 2. Nginx Configuration (`infra/nginx/paga.conf`)

```nginx
server {
    listen 80;
    server_name _;

    # Proxy API and health to Kestrel
    location /api {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        proxy_connect_timeout 5s;
        proxy_read_timeout 30s;
    }

    location /health {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Security headers
    add_header X-Frame-Options DENY always;
    add_header X-Content-Type-Options nosniff always;
    add_header Referrer-Policy strict-origin-when-cross-origin always;
}
```

Key design decisions:
- `proxy_connect_timeout 5s` ensures a quick 502 if Kestrel is unreachable (Req 5.5).
- `/health` gets its own location block to ensure CloudFront `/api/*` pattern isn't the only path (Req 13.1). Note: `/health` also matches Nginx `location /health` directly since it doesn't start with `/api`.
- No SSL termination on Nginx in MVP phase — CloudFront handles viewer HTTPS.

---

### 3. CodeDeploy Lifecycle (`infra/appspec.yml` + `infra/scripts/`)

#### appspec.yml

```yaml
version: 0.0
os: linux
files:
  - source: /
    destination: /opt/paga/api
hooks:
  ApplicationStop:
    - location: infra/scripts/stop.sh
      timeout: 30
  BeforeInstall:
    - location: infra/scripts/install.sh
      timeout: 120
  ApplicationStart:
    - location: infra/scripts/start.sh
      timeout: 30
  ValidateService:
    - location: infra/scripts/validate.sh
      timeout: 60
```

#### stop.sh
- `systemctl stop paga-api.service || true` (graceful, no failure if not running)

#### install.sh
1. Copy published files from deployment artifact to `/opt/paga/api/`
2. Read `DB_PASSWORD` from SSM: `aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text --profile palaia`
3. Construct `psql` connection string for localhost
4. Execute idempotent migration: `psql "$CONN_STRING" -f /opt/paga/api/migrations.sql`

#### start.sh
- `systemctl start paga-api.service`

#### validate.sh
- `curl -sf http://localhost:5000/health` — exit code 0 only if HTTP 200 returned.
- Retry with backoff (up to 30 seconds) to allow Kestrel startup time.

---

### 4. Kestrel Systemd Unit (`paga-api.service`)

Created by EC2 user data:

```ini
[Unit]
Description=PAGA API
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/paga/api
ExecStart=/usr/bin/dotnet /opt/paga/api/Paga.Api.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

Environment variables for secrets are injected from Parameter Store via an `EnvironmentFile` or inline script that reads SSM values at service start.

---

### 5. CORS Production Configuration

The existing `Program.cs` CORS setup already reads from `Cors:AllowedOrigins`:

```csharp
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
              ?? ["http://localhost:4200"];
policy.WithOrigins(origins)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials();
```

**What this spec adds:**

- `appsettings.Production.json` with `Cors:AllowedOrigins` set to the CloudFront distribution domain (`https://dXXXXXXXXXX.cloudfront.net`). The actual domain is filled post-stack-creation.
- The fallback `["http://localhost:4200"]` remains for development.
- Unauthorized origins receive no `Access-Control-Allow-Origin` header — ASP.NET Core's built-in CORS middleware handles this automatically when `WithOrigins` is used.

---

### 6. Operational Documentation

#### Runbook (`docs/runbook.md`)

Sections:
1. **Prerequisites** — AWS CLI, profile `palaia`, Parameter Store secrets created
2. **Stack Creation** — exact `aws cloudformation create-stack` commands in order with all required parameters
3. **Backend Deploy** — `dotnet publish`, generate idempotent migration, scp to EC2, run migration, restart service
4. **Frontend Deploy** — `ng build --configuration production`, `aws s3 sync`, CloudFront invalidation
5. **Validation** — curl `/health` through CloudFront, load SPA, test API call
6. **Troubleshooting** — security group, Nginx, Kestrel, PostgreSQL common failures

#### ADR (`docs/adr/002-single-instance-architecture.md`)

Standard format (Title, Status, Context, Decision, Consequences) documenting the co-location of API + DB + proxy on one t3.micro as an MVP trade-off.

---

## Data Models

This spec introduces no new database entities. The existing entities (User, ExpenseType, Income, Expense, RefreshToken) are migrated to the production PostgreSQL instance via the idempotent migration script.

**Configuration model added:**

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=paga;Username=paga;Password=<from-ssm>"
  },
  "Jwt": {
    "Key": "<from-ssm>"
  },
  "Cors": {
    "AllowedOrigins": ["https://dXXXXXXXXXX.cloudfront.net"]
  },
  "Seed": {
    "AdminEmail": "palaia@increvasenocanal.com",
    "AdminPassword": "<from-ssm>"
  }
}
```

Actual secret values are never committed. The `appsettings.Production.json` on EC2 is generated at deploy time from Parameter Store values, or secrets are passed via environment variables in the systemd unit file.

---

## Interfaces

### Cross-Stack CloudFormation Exports

| Stack | Export Name Pattern | Value |
|-------|-------------------|-------|
| Network | `paga-{Env}-vpc-id` | VPC ID |
| Network | `paga-{Env}-subnet-id` | Public subnet ID |
| Network | `paga-{Env}-sg-id` | Security group ID |
| IAM | `paga-{Env}-instance-profile-arn` | EC2 instance profile ARN |
| IAM | `paga-{Env}-codedeploy-role-arn` | CodeDeploy service role ARN |
| EC2 | `paga-{Env}-ec2-public-ip` | EC2 public IP address |
| Frontend | `paga-{Env}-cloudfront-domain` | CloudFront distribution domain |
| Frontend | `paga-{Env}-cloudfront-distribution-id` | Distribution ID |

### Parameter Store Interface

| Path | Type | Consumer |
|------|------|----------|
| `/paga/jwt-key` | SecureString | EC2 user data / systemd env |
| `/paga/db-password` | SecureString | EC2 user data, install.sh |
| `/paga/admin-password` | SecureString | EC2 user data / systemd env |

### Nginx → Kestrel Interface

| Path Pattern | Upstream | Expected Response |
|-------------|----------|-------------------|
| `/api/*` | `http://localhost:5000` | Application responses |
| `/health` | `http://localhost:5000` | `200 {"status":"Healthy"}` |

---

## Error Handling

### CloudFormation Failures

- **Missing export**: Stack fails with `No export named 'paga-prod-vpc-id' found` — runbook documents creation order to prevent this.
- **Invalid template**: `validate-template` catches syntax errors before any deployment attempt.
- **Parameter Store secret not found**: EC2 user data fails and instance enters a bad state — runbook prereqs section covers secret creation.

### Deploy Failures (CodeDeploy)

| Hook | Failure Mode | Recovery |
|------|-------------|----------|
| ApplicationStop | Service not found | `|| true` in stop.sh prevents failure |
| BeforeInstall | Migration fails | CodeDeploy marks deployment failed; investigate psql error |
| ApplicationStart | Kestrel won't start | Check systemd journal; common cause: missing env vars |
| ValidateService | /health returns non-200 | Check Kestrel logs, DB connection, Nginx forwarding |

### Runtime Errors

| Scenario | Behavior |
|----------|----------|
| Kestrel unreachable | Nginx returns 502 Bad Gateway (proxy_connect_timeout 5s) |
| Unauthorized CORS origin | No `Access-Control-Allow-Origin` header in response |
| DB connection lost | /health returns 503 (degraded); API returns 500 |

---

## File Inventory

```
infra/
├── cloudformation/
│   ├── network.yaml
│   ├── iam.yaml
│   ├── ec2.yaml
│   └── frontend.yaml
├── nginx/
│   └── paga.conf
├── appspec.yml
├── scripts/
│   ├── stop.sh
│   ├── install.sh
│   ├── start.sh
│   └── validate.sh
├── buildspec-frontend.yml      (placeholder for mvp-6)
└── buildspec-backend.yml       (placeholder for mvp-6)

backend/src/Paga.Api/
└── appsettings.Production.json (template — actual values from SSM)

docs/
├── runbook.md
└── adr/
    └── 002-single-instance-architecture.md
```

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Unauthorized origins are rejected

*For any* HTTP origin string that is not present in the configured `Cors:AllowedOrigins` array, a preflight OPTIONS request to any API endpoint SHALL NOT include an `Access-Control-Allow-Origin` header in the response.

**Validates: Requirements 7.5**

---

### Note on Infrastructure Testing Strategy

Requirements 1–6, 8–13 define Infrastructure-as-Code artifacts (CloudFormation templates, Nginx configuration, shell scripts, documentation). These are declarative configurations where:

- Behavior does not vary meaningfully with random input generation
- Running 100 iterations would not find more bugs than a single validation
- The "code under test" is AWS CloudFormation engine behavior, not application logic

Therefore, these requirements are validated through **smoke tests** (template validation, static analysis of YAML/shell content) and **integration tests** (end-to-end deploy validation), not property-based tests. The appropriate verification strategies are:

1. `aws cloudformation validate-template` for each YAML file (Req 1.8, 2.9, 3.8, 4.7)
2. Manual/scripted inspection of template structure for security constraints (Req 1.4–1.6, 4.5, 8.3)
3. Post-deployment integration check: `/health` returns 200 through full path (Req 13.3)
