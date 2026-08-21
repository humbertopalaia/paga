# Implementation Plan: CI/CD Pipeline (mvp-6-cicd)

## Overview

Set up the AWS CI/CD pipeline for PAGA using CodePipeline, CodeBuild, and CodeDeploy. The implementation creates three new files (two buildspecs and a pipeline CloudFormation template) and modifies the existing IAM stack to export the additional role ARNs needed by the pipeline. Each file is validated with `aws cloudformation validate-template` where applicable.

## Tasks

- [x] 1. Update IAM stack with CodePipeline and CodeBuild role exports
  - [x] 1.1 Add CodeStar Connection permission and export CodePipeline/CodeBuild role ARNs
    - Add a `CodeStarConnectionAccess` statement to the CodePipelineRole policy allowing `codestar-connections:UseConnection`
    - Add `CodePipelineRoleArn` output with `Export: Name: !Sub paga-${Environment}-codepipeline-role-arn`
    - Add `CodeBuildRoleArn` output with `Export: Name: !Sub paga-${Environment}-codebuild-role-arn`
    - Validate with `aws cloudformation validate-template --profile palaia --template-body file://infra/cloudformation/iam.yaml`
    - _Requirements: 5.5, 8.3_

- [x] 2. Create frontend buildspec
  - [x] 2.1 Create `infra/buildspec-frontend.yml`
    - Define `install` phase: `runtime-versions: nodejs: 20`, `cd frontend && npm ci`
    - Define `build` phase: `cd frontend && npx ng build --configuration production`
    - Define `post_build` phase: `aws s3 sync frontend/dist/frontend/browser s3://$FRONTEND_BUCKET_NAME --delete` and `aws cloudfront create-invalidation --distribution-id $CLOUDFRONT_DISTRIBUTION_ID --paths "/*"`
    - No artifacts section (frontend deploys inline)
    - Environment variables `FRONTEND_BUCKET_NAME` and `CLOUDFRONT_DISTRIBUTION_ID` are injected by the pipeline CloudFormation template
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 7.1, 7.3_

- [x] 3. Create backend buildspec
  - [x] 3.1 Create `infra/buildspec-backend.yml`
    - Define `install` phase: install `dotnet-ef` tool globally, export PATH
    - Define `pre_build` phase: `cd backend && dotnet restore && dotnet build --no-restore`
    - Define `build` phase: `cd backend && dotnet test --no-build`
    - Define `post_build` phase: `dotnet publish src/Paga.Api -c Release -o $CODEBUILD_SRC_DIR/publish`, generate idempotent migration script to `$CODEBUILD_SRC_DIR/publish/migrations.sql`, copy `appspec.yml` and `infra/scripts/` into publish directory
    - Define `artifacts` section with `base-directory: publish` and `files: "**/*"`
    - Final artifact structure: `appspec.yml`, `migrations.sql`, `infra/scripts/*`, and all published .NET files at root
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 7.2, 7.4, 7.5_

- [x] 4. Checkpoint - Validate buildspec files
  - Ensure buildspec files are syntactically valid YAML. Ask the user if questions arise about runtime images or paths.

- [x] 5. Create pipeline CloudFormation template
  - [x] 5.1 Create `infra/cloudformation/pipeline.yaml` — Parameters and ArtifactBucket
    - Define parameters: `Environment`, `GitHubOwner`, `GitHubRepo`, `GitHubBranch`, `CodeStarConnectionArn`, `ArtifactBucketName`, `FrontendBucketName`, `CloudFrontDistributionId`, `Ec2TagValue`
    - Create `ArtifactBucket` resource (S3, SSE-S3, versioning enabled, no public access)
    - _Requirements: 5.4, 6.1, 6.2_

  - [x] 5.2 Add CodeBuild projects to `pipeline.yaml`
    - Create `FrontendBuild` CodeBuild project: Node 20 image, buildspec path `infra/buildspec-frontend.yml`, environment variables for `FRONTEND_BUCKET_NAME` and `CLOUDFRONT_DISTRIBUTION_ID`, service role from `Fn::ImportValue` of CodeBuild role ARN
    - Create `BackendBuild` CodeBuild project: .NET 10 image (`aws/codebuild/standard:7.0`), buildspec path `infra/buildspec-backend.yml`, service role from `Fn::ImportValue` of CodeBuild role ARN
    - _Requirements: 2.5, 2.6, 3.6, 6.3, 6.4, 8.1, 8.2_

  - [x] 5.3 Add CodeDeploy resources to `pipeline.yaml`
    - Create `CodeDeployApplication` resource (Server compute platform)
    - Create `DeploymentGroup` resource: tag filter on `Name` matching `Ec2TagValue` parameter, service role from `Fn::ImportValue` of CodeDeploy role ARN, `OneAtATime` deployment config
    - _Requirements: 4.1, 6.3, 6.5_

  - [x] 5.4 Add Pipeline resource to `pipeline.yaml`
    - Create `Pipeline` resource with three stages: Source, Build, Deploy
    - Source stage: CodeStarSourceConnection action using `CodeStarConnectionArn` parameter, outputting `SourceOutput`
    - Build stage: two parallel actions (`RunOrder: 1`) — `FrontendBuild` (no output artifact) and `BackendBuild` (output `BackendOutput`)
    - Deploy stage: CodeDeploy action consuming `BackendOutput` artifact
    - Pipeline role from `Fn::ImportValue` of CodePipeline role ARN
    - Artifact store referencing the `ArtifactBucket`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 5.1, 5.2, 5.3, 5.5, 6.4_

  - [x] 5.5 Validate pipeline template
    - Run `aws cloudformation validate-template --profile palaia --template-body file://infra/cloudformation/pipeline.yaml`
    - Fix any validation errors
    - _Requirements: 6.1_

- [x] 6. Final checkpoint - Full validation pass
  - Re-validate both CloudFormation templates (`iam.yaml` and `pipeline.yaml`)
  - Verify all cross-stack reference names match between exports and imports
  - Verify buildspec file paths referenced in CodeBuild projects match actual file locations
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Document deployment steps for the user
  - [x] 7.1 Add deployment instructions as a comment or notes section
    - Document the manual steps the user must follow to deploy: (1) Update IAM stack, (2) Deploy pipeline stack with required parameters, (3) Push to main to trigger first execution
    - Document the CodeStar Connection creation prerequisite (one-time GitHub OAuth in AWS Console)
    - This is documentation only — do NOT execute any `aws cloudformation deploy/update-stack` commands
    - _Requirements: 1.1, 8.4, 8.5_

## Notes

- No property-based tests for this spec — this is pure Infrastructure as Code.
- Template validation (`aws cloudformation validate-template`) serves as the automated verification.
- The actual deployment of these stacks to AWS requires explicit user confirmation and is NOT part of this implementation plan.
- CodeStar Connection must be created manually in the AWS Console (GitHub OAuth) before the pipeline can be deployed.
- The existing `appspec.yml` and `infra/scripts/` are unchanged — the backend buildspec copies them into the artifact.
- All cross-stack references use the naming pattern `paga-${Environment}-<resource>-arn`.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1", "3.1"] },
    { "id": 1, "tasks": ["5.1"] },
    { "id": 2, "tasks": ["5.2", "5.3"] },
    { "id": 3, "tasks": ["5.4"] },
    { "id": 4, "tasks": ["5.5"] },
    { "id": 5, "tasks": ["7.1"] }
  ]
}
```
