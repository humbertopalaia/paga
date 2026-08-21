# Design Document

## Overview

This document describes the architecture for the PAGA CI/CD pipeline (Story PP-111). The pipeline automates the build, test, and deploy cycle for both frontend (Angular SPA → S3 + CloudFront) and backend (.NET API → EC2 via CodeDeploy) using AWS CodePipeline, CodeBuild, and CodeDeploy. All infrastructure is defined as CloudFormation at `infra/cloudformation/pipeline.yaml`.

## Architecture

### Pipeline Topology

```
┌─────────────────────────────────────────────────────────────────────┐
│                        AWS CodePipeline                               │
├────────────┬───────────────────────────────────┬────────────────────┤
│  Stage 1   │           Stage 2                  │      Stage 3       │
│  Source     │           Build                   │      Deploy        │
│            │                                    │                    │
│ GitHub     │  ┌──────────────┐ (parallel)       │  ┌──────────────┐ │
│ (CodeStar) │  │ Frontend     │ RunOrder: 1      │  │ Backend      │ │
│ → artifact │  │ CodeBuild    │──→ S3 sync       │  │ CodeDeploy   │ │
│            │  │ (Node 20)    │──→ CF invalidate │  │ → EC2        │ │
│            │  └──────────────┘                  │  └──────────────┘ │
│            │  ┌──────────────┐ RunOrder: 1      │                    │
│            │  │ Backend      │                  │                    │
│            │  │ CodeBuild    │──→ artifact      │                    │
│            │  │ (.NET 10)    │                  │                    │
│            │  └──────────────┘                  │                    │
└────────────┴───────────────────────────────────┴────────────────────┘
```

**Key design decisions:**

1. **Frontend deploys inline during Build stage** — the frontend CodeBuild project performs `s3 sync` and CloudFront invalidation directly in its post_build phase. No separate deploy stage is needed because there's no CodeDeploy for static files.

2. **Backend produces an artifact** consumed by the Deploy stage via CodeDeploy. The artifact must contain everything CodeDeploy needs: published .NET files, `appspec.yml`, lifecycle scripts, and the migration SQL file.

3. **Parallel builds** — both CodeBuild actions run at `RunOrder: 1` in the Build stage, meaning they execute concurrently from the same source commit.

### Cross-Stack References

```
iam.yaml (existing)
  Exports: codepipeline-role-arn, codebuild-role-arn, codedeploy-role-arn
      ↓ Fn::ImportValue
pipeline.yaml (new)
  Creates: artifact bucket, codebuild projects, codedeploy app/group, pipeline
      ↓ references via
frontend.yaml (existing)
  Exports: cloudfront-distribution-id (for invalidation)
```

The IAM stack currently exports only `codedeploy-role-arn` and `instance-profile-arn`. It must be updated to also export `codepipeline-role-arn` and `codebuild-role-arn`, and the `ArtifactBucketArn` parameter must be supplied when the IAM stack is deployed (the bucket is created in `pipeline.yaml`, so the IAM stack receives the bucket ARN as a parameter during deployment — this is a deploy-time dependency, not a circular reference since the bucket name is parameterized and predictable).

## Components

### 1. CloudFormation Template: `infra/cloudformation/pipeline.yaml`

**Resources defined:**

| Resource | Type | Purpose |
|----------|------|---------|
| `ArtifactBucket` | `AWS::S3::Bucket` | Stores pipeline intermediate artifacts |
| `FrontendBuild` | `AWS::CodeBuild::Project` | Builds Angular SPA, deploys to S3, invalidates CF |
| `BackendBuild` | `AWS::CodeBuild::Project` | Builds/tests .NET API, produces deploy artifact |
| `CodeDeployApplication` | `AWS::CodeDeploy::Application` | Application entity for backend deploys |
| `DeploymentGroup` | `AWS::CodeDeploy::DeploymentGroup` | EC2 target with tag filter `paga-{Env}-ec2` |
| `Pipeline` | `AWS::CodePipeline::Pipeline` | Orchestrates source → build → deploy |

**Parameters:**

```yaml
Parameters:
  Environment:          # prod
  GitHubOwner:          # humbertopalaia
  GitHubRepo:           # paga
  GitHubBranch:         # main
  CodeStarConnectionArn: # arn:aws:codeconnections:...
  ArtifactBucketName:   # paga-prod-pipeline-artifacts
  FrontendBucketName:   # (S3 bucket for SPA, from frontend.yaml)
  CloudFrontDistributionId: # (from frontend.yaml exports)
  Ec2TagValue:          # paga-prod-ec2 (Name tag for CodeDeploy targeting)
```

### 2. Buildspec: `infra/buildspec-frontend.yml`

```yaml
version: 0.2

phases:
  install:
    runtime-versions:
      nodejs: 20
    commands:
      - cd frontend
      - npm ci

  build:
    commands:
      - cd frontend
      - npx ng build --configuration production

  post_build:
    commands:
      - aws s3 sync frontend/dist/frontend/browser s3://$FRONTEND_BUCKET_NAME --delete
      - aws cloudfront create-invalidation --distribution-id $CLOUDFRONT_DISTRIBUTION_ID --paths "/*"
```

**Environment variables** (injected by CloudFormation):
- `FRONTEND_BUCKET_NAME` — S3 bucket hosting the SPA
- `CLOUDFRONT_DISTRIBUTION_ID` — distribution to invalidate

**No output artifact** — the frontend deploys inline.

### 3. Buildspec: `infra/buildspec-backend.yml`

```yaml
version: 0.2

phases:
  install:
    commands:
      - dotnet tool install --global dotnet-ef
      - export PATH="$PATH:$HOME/.dotnet/tools"

  pre_build:
    commands:
      - cd backend
      - dotnet restore
      - dotnet build --no-restore

  build:
    commands:
      - cd backend
      - dotnet test --no-build

  post_build:
    commands:
      - cd backend
      - dotnet publish src/Paga.Api -c Release -o $CODEBUILD_SRC_DIR/publish
      - dotnet ef migrations script --idempotent --project src/Paga.Infrastructure --startup-project src/Paga.Api -o $CODEBUILD_SRC_DIR/publish/migrations.sql

artifacts:
  base-directory: publish
  files:
    - "**/*"
  secondary-artifacts: {}
  discard-paths: no
```

**Artifact layout detail:** The artifact needs `appspec.yml` and `infra/scripts/` at the root. Since the source action provides the full repo, and `dotnet publish` outputs to `publish/`, the buildspec must copy `appspec.yml` and `infra/scripts/` into the publish directory before declaring it as the artifact base:

```yaml
  post_build:
    commands:
      - cd backend
      - dotnet publish src/Paga.Api -c Release -o $CODEBUILD_SRC_DIR/publish
      - dotnet ef migrations script --idempotent --project src/Paga.Infrastructure --startup-project src/Paga.Api -o $CODEBUILD_SRC_DIR/publish/migrations.sql
      - cp $CODEBUILD_SRC_DIR/infra/appspec.yml $CODEBUILD_SRC_DIR/publish/appspec.yml
      - cp -r $CODEBUILD_SRC_DIR/infra/scripts $CODEBUILD_SRC_DIR/publish/infra/scripts
```

This produces the flat artifact structure CodeDeploy expects:

```
publish/           (artifact base-directory)
├── appspec.yml
├── migrations.sql
├── infra/scripts/stop.sh
├── infra/scripts/install.sh
├── infra/scripts/start.sh
├── infra/scripts/validate.sh
├── Paga.Api.dll
├── (other published .NET files)
```

### 4. CodeDeploy Lifecycle (existing, unchanged)

The existing `appspec.yml` and scripts remain as-is:

| Hook | Script | Action |
|------|--------|--------|
| `ApplicationStop` | `infra/scripts/stop.sh` | `systemctl stop paga-api.service` |
| `BeforeInstall` | `infra/scripts/install.sh` | Reads DB password from SSM, runs `psql -f migrations.sql` |
| `ApplicationStart` | `infra/scripts/start.sh` | `systemctl start paga-api.service` |
| `ValidateService` | `infra/scripts/validate.sh` | Retries `curl /health` with backoff up to 30s |

### 5. IAM Stack Updates (`iam.yaml`)

The existing IAM stack must export two additional role ARNs:

```yaml
Outputs:
  # existing
  InstanceProfileArn: ...
  CodeDeployRoleArn: ...
  # new exports
  CodePipelineRoleArn:
    Description: ARN of the CodePipeline service role
    Value: !GetAtt CodePipelineRole.Arn
    Export:
      Name: !Sub paga-${Environment}-codepipeline-role-arn
  CodeBuildRoleArn:
    Description: ARN of the CodeBuild service role
    Value: !GetAtt CodeBuildRole.Arn
    Export:
      Name: !Sub paga-${Environment}-codebuild-role-arn
```

Additionally, the CodePipeline role needs a `codestar-connections:UseConnection` permission to allow the source action to use the CodeStar Connection:

```yaml
- Sid: CodeStarConnectionAccess
  Effect: Allow
  Action:
    - codestar-connections:UseConnection
  Resource: "*"  # Scoped at deploy time; connection ARN is dynamic
```

## Data Flow

```
1. Developer pushes to main
     ↓
2. CodeStar Connection webhook triggers Pipeline
     ↓
3. Source Stage: fetch full repo → SourceOutput artifact
     ↓
4a. Frontend Build (parallel):
    npm ci → ng build → s3 sync → CF invalidation
    (no output artifact)

4b. Backend Build (parallel):
    dotnet restore → build → test → publish → ef migrations script
    → copy appspec + scripts → BackendOutput artifact
     ↓
5. Deploy Stage:
    CodeDeploy picks up BackendOutput
    → ApplicationStop (stop service)
    → BeforeInstall (apply migrations from SSM creds)
    → files copied to /opt/paga/api
    → ApplicationStart (start service)
    → ValidateService (health check with retry)
     ↓
6. Pipeline green ✓
```

## Error Handling

| Failure Point | Behavior |
|---------------|----------|
| Frontend `npm ci` or `ng build` fails | Build stage fails, pipeline stops. No partial deploy. |
| Backend `dotnet test` fails | Build stage fails, pipeline stops. No deploy. |
| Backend `dotnet publish` fails | Build stage fails. |
| Migration script fails (`psql` in BeforeInstall) | CodeDeploy deployment fails. EC2 state unchanged (service was stopped but app files not yet replaced at BeforeInstall). |
| Service fails to start | ValidateService hook fails → deployment marked failed by CodeDeploy. |
| Health check timeout (30s) | validate.sh exits 1 → deployment marked failed. |

**Rollback strategy:** CodeDeploy is configured with `OneAtATime` deployment configuration. On failure, the previous revision remains in `/opt/paga/api` if the failure occurs before file copy (BeforeInstall), otherwise manual rollback via CodeDeploy console. In MVP phase, no automatic rollback is configured since there's a single instance.

## Security Considerations

1. **No secrets in templates or buildspecs** — database credentials are read exclusively from SSM Parameter Store by the `install.sh` hook at deploy time.
2. **Least-privilege IAM** — each role (CodePipeline, CodeBuild, CodeDeploy, EC2) has only the permissions it needs. The CodeBuild role can write to the frontend S3 bucket and create CloudFront invalidations (for the frontend build), plus the artifact bucket and CloudWatch Logs.
3. **Source connection** — the CodeStar Connection ARN is parameterized, never hardcoded. The connection is created manually in the AWS Console (one-time GitHub OAuth) and its ARN passed to the template.
4. **Artifact bucket** — encrypted with SSE-S3, versioning enabled, no public access.

## Files to Create/Modify

| File | Action | Description |
|------|--------|-------------|
| `infra/cloudformation/pipeline.yaml` | **Create** | Full pipeline infrastructure |
| `infra/buildspec-frontend.yml` | **Create** | Frontend build + deploy spec |
| `infra/buildspec-backend.yml` | **Create** | Backend build + artifact spec |
| `infra/cloudformation/iam.yaml` | **Modify** | Add exports for CodePipeline and CodeBuild role ARNs; add CodeStar Connection permission |

**No changes** to: `appspec.yml`, `infra/scripts/*`, `infra/nginx/paga.conf`.

## Correctness Properties

*This is an Infrastructure as Code (IaC) project. All acceptance criteria involve declarative configuration (CloudFormation templates, buildspec YAML files, shell scripts) rather than functions with inputs/outputs. Property-based testing is not appropriate for IaC — there are no pure functions, no meaningful input variation, and no universal properties that benefit from 100+ random iterations.*

*The appropriate testing strategy for this spec is:*
- **Template validation**: `aws cloudformation validate-template` on all templates
- **Static assertions**: Verify specific resource properties, parameter declarations, and cross-stack references exist in the templates
- **Integration tests**: Deploy and verify the pipeline triggers, builds, and deploys correctly (1-2 manual runs)

*No property-based tests are generated for this feature.*
