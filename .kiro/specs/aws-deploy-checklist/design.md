# Design Document — AWS Deploy Checklist

## Overview

This document designs an **executable operational checklist** (not application code) for provisioning and deploying the PAGA MVP on AWS. The output artifact is a structured markdown document with exact commands, decision points, and validation gates that an Operator executes sequentially from a Windows + PowerShell workstation.

The checklist materializes the runbook (`docs/runbook.md`) into a self-contained task list with:
- Exact commands using generic placeholders
- Clear execution context markers (local vs. EC2)
- Data flow annotations showing where captured values feed into later steps
- Error handling branches at each critical gate

---

## Execution Flow

The deploy follows a strict linear sequence with conditional branches only for error handling. No step may be skipped; each depends on outputs from the previous.

```
┌─────────────────────────────────────────────────────────────────────┐
│  PHASE 1: Prerequisites (Local)                                     │
│  Verify tools → Create secrets → Record operator inputs             │
├─────────────────────────────────────────────────────────────────────┤
│  PHASE 2: Infrastructure Provisioning (Local → AWS)                 │
│  Network → IAM → EC2 → Frontend                                    │
│  Each: create-stack → wait → [capture outputs]                      │
├─────────────────────────────────────────────────────────────────────┤
│  PHASE 3: Backend Deploy (Local build → SCP → EC2 execution)        │
│  Publish → Generate migration SQL → Transfer → SSH → Migrate        │
│  → Configure .env → Start service                                   │
├─────────────────────────────────────────────────────────────────────┤
│  PHASE 4: Frontend Deploy (Local build → S3 → CloudFront)           │
│  ng build → s3 sync → invalidate cache                              │
├─────────────────────────────────────────────────────────────────────┤
│  PHASE 5: End-to-End Validation (Local)                             │
│  Health → SPA → Login → CORS → Done                                 │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow

Values captured in early steps propagate forward. The checklist must make this explicit with placeholder variables.

| Captured Value | Captured At | Used In |
|----------------|-------------|---------|
| `<your-ip>/32` | Phase 1 (Operator input) | Network stack `SshCidr` |
| `<your-key-pair-name>` | Phase 1 (Operator input) | EC2 stack `KeyPairName`, SSH/SCP commands |
| `<your-spa-bucket-name>` | Phase 1 (Operator input) | Frontend stack `BucketName`, S3 sync |
| `<path-to-private-key.pem>` | Phase 1 (Operator input) | SCP and SSH commands |
| `<account-id>` | Phase 1 (from `sts get-caller-identity`) | IAM stack `CloudFrontDistributionArn` |
| `<ec2-public-ip>` | Phase 2 EC2 stack outputs | Frontend stack `Ec2PublicIp`, SCP, SSH |
| `<cloudfront-domain>` | Phase 2 Frontend stack outputs | Backend CORS config, all validation curls |
| `<distribution-id>` | Phase 2 Frontend stack outputs | CloudFront invalidation |

### Placeholder Convention

- Angle brackets `<placeholder>` denote values the Operator fills in
- Once captured (e.g., EC2 IP from stack output), subsequent uses reference the same placeholder
- Secrets are never displayed — they go directly from Parameter Store into commands via shell variables

---

## Decision Points and Branching Logic

### First Deploy vs. Re-deploy

The checklist is written for a **first deploy** scenario. Re-deploy differences:

| Concern | First Deploy | Re-deploy |
|---------|-------------|-----------|
| Parameter Store secrets | Must create all three | Skip (already exist) |
| Stack creation | `create-stack` | `update-stack` (not covered in this checklist) |
| CloudFront Distribution ARN | Wildcard `arn:aws:cloudfront::<account-id>:distribution/*` | Use actual distribution ARN from outputs |
| Database migration | Creates all tables | Idempotent script handles delta safely |
| `.env` file on EC2 | Create new | Overwrite existing |

The checklist addresses the first-deploy chicken-and-egg problem explicitly:

1. IAM stack needs `CloudFrontDistributionArn` but the Frontend stack doesn't exist yet
2. Solution: pass a wildcard ARN (`arn:aws:cloudfront::<account-id>:distribution/*`) on first deploy
3. After Frontend stack creates the distribution, the Operator can optionally update the IAM stack with the actual ARN (not required for MVP validation)

### Error Handling at Each Gate

Every `create-stack` command is followed by a `wait` command. If a stack fails:

```
┌─────────────────────────────┐
│ aws cloudformation wait ... │
│         ↓                   │
│  Exit code 0?              │
│    YES → continue           │
│    NO  → describe-stack-    │
│          events → diagnose  │
│          → fix → retry      │
└─────────────────────────────┘
```

For the backend service start:

```
┌─────────────────────────────┐
│ systemctl restart paga-api  │
│         ↓                   │
│ systemctl status            │
│         ↓                   │
│  Active (running)?          │
│    YES → continue           │
│    NO  → journalctl -u      │
│          paga-api -n 50     │
│          → diagnose → fix   │
└─────────────────────────────┘
```

---

## Execution Context

Each step runs in one of two contexts. The checklist must annotate every command block.

| Context | Shell | Indicator in Checklist |
|---------|-------|----------------------|
| **Local** | PowerShell on Windows | `[LOCAL]` prefix |
| **Remote (EC2)** | Bash on Amazon Linux 2023 | `[EC2]` prefix, entered via `ssh` |

### Context Transitions

1. **Local → Remote:** `ssh -i <key.pem> ec2-user@<ec2-public-ip>` opens a remote session
2. **Remote → Local:** `exit` closes the SSH session; subsequent commands are local again
3. **SCP** is a local command that transfers files to remote without opening a session

The checklist must clearly mark when the Operator enters and exits the SSH session.

---

## Phase Design Details

### Phase 1: Prerequisites Verification

**Context:** Local (PowerShell)

Steps:
1. Verify tools (aws, dotnet, node, ng) — each with expected version output
2. Verify AWS profile identity — `aws sts get-caller-identity --profile palaia`
3. Record operator inputs (IP, key pair, bucket name) in a "Variables" section at the top
4. Create Parameter Store secrets (idempotent: `--overwrite` flag not used, will fail if they exist — intentional for first deploy)

**Error branch:** If any tool is missing, stop and instruct the Operator to install it before proceeding.

### Phase 2: Infrastructure Provisioning

**Context:** Local (PowerShell)

Strict order enforced by cross-stack `Fn::ImportValue` dependencies:

```
Network (VPC, Subnet, SG)
    ↓ exports: subnet-id, sg-id
IAM (Roles, Instance Profile)
    ↓ exports: instance-profile-arn
EC2 (Instance)
    ↓ outputs: Ec2PublicIp
Frontend (S3, CloudFront)
    ↓ outputs: CloudFrontDomain, CloudFrontDistributionId
```

Each stack block:
1. `create-stack` with all parameters
2. `wait stack-create-complete`
3. Error branch: `describe-stack-events` on failure
4. Output capture: `describe-stacks --query` for needed values

### Phase 3: Backend Deploy

**Context:** Mixed — starts Local, transitions to Remote

| Step | Context | Purpose |
|------|---------|---------|
| `dotnet publish` | Local | Compile release artifact |
| `dotnet ef migrations script` | Local | Generate idempotent SQL |
| `scp` | Local | Transfer artifacts to EC2 |
| `ssh` | Transition | Enter remote context |
| Read secrets from Parameter Store | EC2 | Get credentials for DB and .env |
| `psql -f migrations.sql` | EC2 | Apply schema |
| Write `.env` file | EC2 | Configure runtime environment |
| `chmod 600` | EC2 | Secure the env file |
| `systemctl restart` | EC2 | Start the service |
| `systemctl status` | EC2 | Verify it's running |
| Error branch: `journalctl` | EC2 | Diagnose failures |
| `exit` | Transition | Return to local context |

### Phase 4: Frontend Deploy

**Context:** Local (PowerShell)

1. `ng build --configuration production` from `frontend/` directory
2. `aws s3 sync` with `--delete` to remove stale files
3. Retrieve distribution ID from stack outputs
4. `aws cloudfront create-invalidation` for `/*`
5. Note: wait 1-5 minutes for propagation

### Phase 5: End-to-End Validation

**Context:** Local (PowerShell + browser)

Sequential validation gates — each must pass before proceeding:

1. **Health check:** `curl` to CloudFront `/api/health` → expect `{"status":"Healthy"}`
2. **SPA load:** Open CloudFront URL in browser → login page renders
3. **Auth flow:** `curl POST /api/auth/login` with admin credentials → 200 + TokenResponse
4. **CORS verification:** `curl OPTIONS` with Origin header → `Access-Control-Allow-Origin` present
5. **Completion declaration:** All gates passed = deploy is successful

---

## Error Handling Strategy

| Phase | Failure Mode | Action |
|-------|-------------|--------|
| Prerequisites | Tool not installed | Stop, instruct install |
| Prerequisites | Profile not configured | Stop, instruct `aws configure --profile palaia` |
| Stack creation | CREATE_FAILED | `describe-stack-events`, fix template/params, `delete-stack`, retry |
| SCP transfer | Connection refused | Verify EC2 is running, SG allows SSH from operator IP |
| Migration | psql error | Check DB user permissions, verify idempotent script |
| Service start | systemctl failed | `journalctl` for details — common: missing .env, wrong connection string |
| Health check | Non-200 | Check Nginx → Kestrel chain, verify port 5000 listening |
| Login test | 401 | Verify admin was seeded, check seed configuration in .env |
| CORS check | Missing header | Verify `Cors__AllowedOrigins__0` matches CloudFront domain exactly |

---

## Security Constraints

1. All `aws` CLI commands include `--profile palaia` — no default profile usage
2. Secrets flow from Parameter Store to shell variables to `.env` file — never echoed, never in command history
3. `.env` file is `chmod 600` (owner-read only)
4. SSH key path uses a placeholder — never committed
5. Port 22 restricted to operator's IP via `SshCidr` parameter
6. Port 80 restricted to CloudFront prefix list — no direct public HTTP access to EC2
7. PostgreSQL (5432) never exposed — localhost only

---

## Checklist Document Format

The generated checklist artifact follows this structure:

```markdown
# PAGA MVP — Deploy Checklist

## Variables (fill before starting)
| Variable | Value |
|----------|-------|
| `<your-ip>/32` | |
| ... | |

## Phase 1: Prerequisites [LOCAL]
### 1.1 Verify tools
...
### 1.2 Create Parameter Store secrets
...

## Phase 2: Infrastructure [LOCAL]
### 2.1 Network Stack
...

## Phase 3: Backend Deploy [LOCAL → EC2]
...

## Phase 4: Frontend Deploy [LOCAL]
...

## Phase 5: Validation [LOCAL]
...

## Deployment Complete
```

Each command block is annotated with `[LOCAL]` or `[EC2]` context markers. Conditional branches use blockquotes with a warning icon for error paths.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: AWS CLI profile consistency

*For any* `aws` CLI command appearing in the generated checklist, that command SHALL contain the flag `--profile palaia`.

**Validates: Requirements 1.8**

### Property 2: Data flow completeness — forward reference integrity

*For any* placeholder variable used in a command (e.g., `<ec2-public-ip>`), there SHALL exist a prior step in the checklist that captures or defines that variable.

**Validates: Requirements 1.7, 4.3, 5.3, 5.4**

### Property 3: Stack ordering respects dependency chain

*For any* pair of stacks where stack B imports exports from stack A, the checklist SHALL position the `create-stack` command for A before the `create-stack` command for B.

**Validates: Requirements 2.1, 3.1, 4.1, 5.1**

### Property 4: Every create-stack has a corresponding wait

*For any* `aws cloudformation create-stack` command in the checklist, there SHALL exist an immediately subsequent `aws cloudformation wait stack-create-complete` command for the same stack name.

**Validates: Requirements 2.2, 3.3, 4.2, 5.2**

### Property 5: Remote commands execute within SSH context

*For any* command that must run on the EC2 instance (bash commands, psql, systemctl), that command SHALL appear within an SSH session block — after an `ssh` connection command and before the corresponding `exit`.

**Validates: Requirements 7.1, 7.2, 7.3, 8.1, 8.2, 8.3, 8.4, 8.5**
