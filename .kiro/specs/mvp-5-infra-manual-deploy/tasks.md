# Implementation Plan: mvp-5-infra-manual-deploy

## Overview

Provision all AWS infrastructure artifacts for the PAGA MVP walking skeleton: CloudFormation templates (network, IAM, EC2, frontend), Nginx reverse proxy config, CodeDeploy lifecycle scripts, production CORS configuration, and operational documentation (runbook + ADR). All templates are validated offline with `aws cloudformation validate-template --profile palaia` — no real AWS resources are created during implementation.

## Tasks

- [x] 1. Create Network Stack CloudFormation template
  - [x] 1.1 Create `infra/cloudformation/network.yaml` with VPC, public subnet, internet gateway, route table, and security group
    - VPC CIDR from parameter (default `10.0.0.0/16`)
    - One public subnet in AZ `a`
    - Internet gateway + route table with `0.0.0.0/0` route
    - Security group: ports 80/443 from CloudFront prefix list (`com.amazonaws.global.cloudfront.origin-facing`), port 22 from `SshCidr` parameter (required, no default)
    - Port 5432 never opened
    - Parameters: `Environment` (default `prod`), `VpcCidr` (default `10.0.0.0/16`), `SshCidr` (required)
    - Exports: `paga-{Environment}-vpc-id`, `paga-{Environment}-subnet-id`, `paga-{Environment}-sg-id`
    - Resource naming convention: `paga-{Environment}-<resource>`
    - Run `aws cloudformation validate-template --template-body file://infra/cloudformation/network.yaml --profile palaia`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 12.1, 12.2, 12.3_

- [x] 2. Create IAM Stack CloudFormation template
  - [x] 2.1 Create `infra/cloudformation/iam.yaml` with four IAM roles and an instance profile
    - CodePipelineRole: invoke CodeBuild/CodeDeploy, read/write artifact S3 bucket
    - CodeBuildRole: read source/output artifact, CloudWatch Logs, S3 sync for frontend bucket, CloudFront invalidation
    - CodeDeployRole: manage EC2 deployments, read deployment artifacts from S3
    - EC2InstanceRole + InstanceProfile: SSM GetParameter on `/paga/*`, S3 artifact bucket read
    - No wildcard `*` in Resource when ARN is known/parameterized
    - Parameters: `Environment` (default `prod`), `ArtifactBucketArn` (required), `FrontendBucketArn` (required), `CloudFrontDistributionArn` (required)
    - Exports: `paga-{Environment}-instance-profile-arn`, `paga-{Environment}-codedeploy-role-arn`
    - Run `aws cloudformation validate-template --template-body file://infra/cloudformation/iam.yaml --profile palaia`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 12.1, 12.2, 12.3_

- [x] 3. Create EC2 Stack CloudFormation template
  - [x] 3.1 Create `infra/cloudformation/ec2.yaml` with EC2 instance, user data bootstrap, and systemd unit
    - Instance type from parameter (default `t3.micro`), AMI via SSM public parameter (Amazon Linux 2023)
    - Import VPC subnet + security group from Network stack, instance profile from IAM stack using `Fn::ImportValue`
    - `AssociatePublicIpAddress: true`
    - User data script installs: .NET 10 runtime, PostgreSQL 16 server + psql, Nginx, CodeDeploy agent
    - Configures PostgreSQL: `listen_addresses = 'localhost'`, creates database `paga`, reads password from SSM `/paga/db-password`
    - Creates `/opt/paga/api` directory
    - Creates systemd unit `paga-api.service` (Kestrel on localhost:5000, env `Production`)
    - Enables services on boot: postgresql, nginx, codedeploy-agent
    - Parameters: `Environment` (default `prod`), `InstanceType` (default `t3.micro`), `KeyPairName` (required)
    - Export: `paga-{Environment}-ec2-public-ip`
    - Run `aws cloudformation validate-template --template-body file://infra/cloudformation/ec2.yaml --profile palaia`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 8.2, 8.3, 9.2, 12.1, 12.2, 12.3_

- [x] 4. Create Frontend Stack CloudFormation template
  - [x] 4.1 Create `infra/cloudformation/frontend.yaml` with S3 bucket, OAC, CloudFront distribution
    - S3 bucket with parameterized name, bucket policy allowing only CloudFront OAC
    - Origin Access Control for S3
    - CloudFront distribution: default behavior → S3 (OAC, CachingOptimized), `/api/*` → EC2 HTTP :80 (CachingDisabled, all forwarded)
    - SPA fallback: CustomErrorResponses for 403/404 → `/index.html` with status 200
    - No custom domain, no ACM certificate, no Aliases
    - EC2 public IP accepted as parameter for API origin
    - Parameters: `Environment` (default `prod`), `BucketName` (required), `Ec2PublicIp` (required)
    - Exports: `paga-{Environment}-cloudfront-domain`, `paga-{Environment}-cloudfront-distribution-id`
    - Run `aws cloudformation validate-template --template-body file://infra/cloudformation/frontend.yaml --profile palaia`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 9.3, 12.1, 12.2, 12.3, 13.2_

- [x] 5. Checkpoint - Validate all CloudFormation templates
  - Ensure all four templates pass `aws cloudformation validate-template --profile palaia`, ask the user if questions arise.

- [x] 6. Create Nginx and CodeDeploy artifacts
  - [x] 6.1 Create `infra/nginx/paga.conf` with reverse proxy configuration
    - Listen on port 80, `server_name _`
    - Location `/api`: proxy_pass to `http://localhost:5000`, forward Host/X-Real-IP/X-Forwarded-For/X-Forwarded-Proto headers
    - Location `/health`: same proxy_pass configuration
    - `proxy_connect_timeout 5s` for quick 502 on Kestrel unreachable
    - Security headers: X-Frame-Options DENY, X-Content-Type-Options nosniff, Referrer-Policy strict-origin-when-cross-origin
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 13.1_

  - [x] 6.2 Create `infra/appspec.yml` with CodeDeploy application specification
    - `os: linux`, file mapping from `/` to `/opt/paga/api`
    - Hooks: ApplicationStop → stop.sh (30s), BeforeInstall → install.sh (120s), ApplicationStart → start.sh (30s), ValidateService → validate.sh (60s)
    - _Requirements: 6.1, 6.2_

  - [x] 6.3 Create `infra/scripts/stop.sh`
    - `systemctl stop paga-api.service || true` (graceful, no failure if not running)
    - _Requirements: 6.3_

  - [x] 6.4 Create `infra/scripts/install.sh`
    - Copy published files to `/opt/paga/api/`
    - Read `DB_PASSWORD` from SSM using `aws ssm get-parameter --name /paga/db-password --with-decryption`
    - Construct psql connection string for localhost
    - Execute idempotent migration: `psql "$CONN_STRING" -f /opt/paga/api/migrations.sql`
    - _Requirements: 6.4, 6.7, 8.2_

  - [x] 6.5 Create `infra/scripts/start.sh`
    - `systemctl start paga-api.service`
    - _Requirements: 6.5_

  - [x] 6.6 Create `infra/scripts/validate.sh`
    - `curl -sf http://localhost:5000/health` with retry and backoff (up to 30s)
    - Exit 0 only if HTTP 200 returned
    - _Requirements: 6.6, 13.3_

- [x] 7. Create production CORS configuration
  - [x] 7.1 Create `backend/src/Paga.Api/appsettings.Production.json`
    - `Cors:AllowedOrigins` with placeholder CloudFront domain (`https://dXXXXXXXXXX.cloudfront.net`)
    - `ConnectionStrings:Default` with placeholder (actual value from SSM at deploy time)
    - `Jwt:Key` placeholder
    - `Seed:AdminEmail` = `palaia@increvasenocanal.com`, `Seed:AdminPassword` placeholder
    - Comment that actual secret values are injected from Parameter Store, never committed
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.3_

- [x] 8. Checkpoint - Review scripts and configuration
  - Ensure all scripts have correct shebang, are executable-ready, and appspec.yml references the correct paths. Ask the user if questions arise.

- [x] 9. Create operational documentation
  - [x] 9.1 Create `docs/runbook.md` with manual deploy instructions
    - Prerequisites: AWS CLI, profile `palaia`, Parameter Store secrets (`/paga/jwt-key`, `/paga/db-password`, `/paga/admin-password`)
    - Stack creation commands in order: Network → IAM → EC2 → Frontend, with all required parameters
    - Backend deploy: `dotnet publish`, generate idempotent migration script, scp to EC2, run migration, restart service
    - Frontend deploy: `ng build --configuration production`, `aws s3 sync`, CloudFront invalidation
    - Validation: curl `/health` through CloudFront, load SPA, test API call
    - Troubleshooting: security group blocking, Nginx misconfiguration, Kestrel not starting, database connection failure
    - _Requirements: 8.1, 9.1, 9.4, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6_

  - [x] 9.2 Create `docs/adr/002-single-instance-architecture.md`
    - Standard ADR format: Title, Status (Accepted), Context, Decision, Consequences
    - Document co-location of .NET API + PostgreSQL + Nginx on single EC2 t3.micro
    - State MVP/validation phase acceptability
    - List trade-offs: single point of failure, no horizontal scaling, no managed database
    - Reference future path: RDS, ECS/Fargate, or multi-instance
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

- [x] 10. Final checkpoint - Full review
  - Ensure all files are consistent with each other (cross-stack exports/imports match, appspec paths align with scripts, Nginx config aligns with EC2 user data). Ask the user if questions arise.

## Notes

- This spec produces only declarative artifacts (YAML, shell scripts, Nginx config, documentation). No application logic is modified except for adding `appsettings.Production.json`.
- CloudFormation templates are validated offline — no AWS resources are created.
- The CORS property test (Property 1 from design) is already covered by the existing CORS middleware configuration in the backend; `appsettings.Production.json` simply supplies the production origin value.
- Scripts assume the CodeDeploy agent delivers the deployment artifact to a staging directory and `install.sh` moves files to `/opt/paga/api/`.
- The `buildspec-frontend.yml` and `buildspec-backend.yml` are placeholders for `mvp-6-cicd` and are NOT created in this spec.
- The stack creation order (Network → IAM → EC2 → Frontend) is critical and enforced by cross-stack `Fn::ImportValue` references.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1"] },
    { "id": 1, "tasks": ["3.1", "6.1"] },
    { "id": 2, "tasks": ["4.1", "6.2", "6.3", "6.4", "6.5", "6.6"] },
    { "id": 3, "tasks": ["7.1", "9.2"] },
    { "id": 4, "tasks": ["9.1"] }
  ]
}
```
