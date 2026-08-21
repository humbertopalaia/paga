# Requirements Document

## Introduction

CI/CD pipeline for the PAGA application using AWS CodePipeline, CodeBuild, and CodeDeploy. The pipeline automates building, testing, and deploying both the Angular frontend (to S3 + CloudFront) and the .NET backend (to EC2 via CodeDeploy) on every push to the `main` branch. This eliminates the manual deploy process used during the MVP walking skeleton phase.

## Glossary

- **Pipeline**: AWS CodePipeline orchestrating source, build, and deploy stages for the PAGA application.
- **Frontend_Build**: AWS CodeBuild project that installs dependencies, builds the Angular SPA, syncs to S3, and invalidates CloudFront.
- **Backend_Build**: AWS CodeBuild project that restores, builds, tests, publishes the .NET API, generates the migration script, and packages the deployment artifact.
- **Backend_Deploy**: AWS CodeDeploy deployment that installs the API artifact on EC2 using lifecycle hooks.
- **Source_Stage**: Pipeline stage that fetches source code from GitHub via CodeStar Connection on push to `main`.
- **Artifact_Bucket**: S3 bucket used by CodePipeline for intermediate build artifacts between stages.
- **CodeStar_Connection**: AWS CodeStar Connections resource linking the pipeline to the GitHub repository (created manually, ARN passed as parameter).
- **Deployment_Group**: CodeDeploy deployment group targeting the PAGA EC2 instance tagged by environment.

## Requirements

### Requirement 1: Pipeline Source Integration

**User Story:** As a developer, I want the pipeline to trigger automatically on push to `main`, so that every merged change is deployed without manual intervention.

#### Acceptance Criteria

1. WHEN a commit is pushed to the `main` branch of the GitHub repository, THE Pipeline SHALL start a new execution automatically.
2. THE Source_Stage SHALL use a CodeStar_Connection to fetch the full repository source code.
3. THE Pipeline SHALL accept the CodeStar_Connection ARN as a CloudFormation parameter, without hardcoding connection identifiers.
4. THE Source_Stage SHALL produce an output artifact containing the complete repository source for consumption by subsequent stages.

### Requirement 2: Frontend Build and Deploy

**User Story:** As a developer, I want the frontend to be built, synced to S3, and have CloudFront invalidated automatically, so that users see the latest UI after each deploy.

#### Acceptance Criteria

1. WHEN the Frontend_Build starts, THE Frontend_Build SHALL install dependencies using `npm ci` from the `frontend/` directory.
2. WHEN dependencies are installed, THE Frontend_Build SHALL execute `ng build --configuration production` to produce the production bundle.
3. WHEN the production build succeeds, THE Frontend_Build SHALL sync the `dist/` output to the S3 bucket hosting the SPA using `aws s3 sync --delete`.
4. WHEN the S3 sync completes, THE Frontend_Build SHALL create a CloudFront invalidation for `/*` to clear cached content.
5. THE Frontend_Build SHALL receive the S3 bucket name and CloudFront distribution ID as environment variables, without hardcoding resource identifiers.
6. THE Frontend_Build SHALL use a Node.js 20 runtime image compatible with Angular 19.

### Requirement 3: Backend Build and Artifact

**User Story:** As a developer, I want the backend to be built, tested, and packaged as a deployment artifact, so that CodeDeploy can install it on EC2.

#### Acceptance Criteria

1. WHEN the Backend_Build starts, THE Backend_Build SHALL restore NuGet packages and build the solution from the `backend/` directory.
2. WHEN the build succeeds, THE Backend_Build SHALL run `dotnet test` to execute all unit and integration tests.
3. WHEN tests pass, THE Backend_Build SHALL execute `dotnet publish` in Release configuration to produce the self-contained API output.
4. WHEN publish completes, THE Backend_Build SHALL generate an idempotent migration script using `dotnet ef migrations script --idempotent`.
5. THE Backend_Build SHALL produce an output artifact containing the published API, `appspec.yml` at the root, `infra/scripts/` at `infra/scripts/`, and `migrations.sql` at the root level expected by the install hook.
6. THE Backend_Build SHALL use a .NET 10 SDK image.

### Requirement 4: Backend Deploy via CodeDeploy

**User Story:** As a developer, I want CodeDeploy to install and restart the API on EC2 with database migration, so that the latest backend is live after each pipeline run.

#### Acceptance Criteria

1. WHEN the Backend_Deploy begins, THE Backend_Deploy SHALL use the existing `appspec.yml` lifecycle hooks (ApplicationStop, BeforeInstall, ApplicationStart, ValidateService).
2. WHEN the ApplicationStop hook runs, THE Backend_Deploy SHALL stop the `paga-api` systemd service gracefully.
3. WHEN the BeforeInstall hook runs, THE Backend_Deploy SHALL apply the idempotent migration script using `psql` with credentials from SSM Parameter Store.
4. WHEN the ApplicationStart hook runs, THE Backend_Deploy SHALL start the `paga-api` systemd service.
5. WHEN the ValidateService hook runs, THE Backend_Deploy SHALL verify the `/health` endpoint responds with HTTP 200 within 30 seconds.
6. IF the ValidateService hook fails, THEN THE Backend_Deploy SHALL mark the deployment as failed.

### Requirement 5: Pipeline Architecture

**User Story:** As a developer, I want a single pipeline with parallel build stages, so that frontend and backend deploy from the same commit efficiently.

#### Acceptance Criteria

1. THE Pipeline SHALL consist of three stages: Source, Build, and Deploy.
2. THE Pipeline SHALL execute the Frontend_Build and Backend_Build as parallel actions within the Build stage, so that both build from the same source commit.
3. THE Pipeline SHALL execute the Backend_Deploy in the Deploy stage after the Build stage completes.
4. THE Pipeline SHALL create its own Artifact_Bucket with a parameterized name.
5. THE Pipeline SHALL reference IAM roles from the existing `iam.yaml` stack exports (CodePipeline role, CodeBuild role, CodeDeploy role).

### Requirement 6: CloudFormation Template

**User Story:** As a developer, I want the pipeline infrastructure defined as CloudFormation, so that it is reproducible and version-controlled.

#### Acceptance Criteria

1. THE Pipeline SHALL be defined in a CloudFormation template at `infra/cloudformation/pipeline.yaml`.
2. THE Pipeline template SHALL parameterize: environment name, GitHub repository owner, repository name, branch name, CodeStar Connection ARN, artifact bucket name, frontend S3 bucket name, CloudFront distribution ID, and EC2 tag filters for CodeDeploy.
3. THE Pipeline template SHALL define the CodeBuild projects, CodeDeploy application, Deployment_Group, Artifact_Bucket, and the Pipeline resource itself.
4. THE Pipeline template SHALL use cross-stack references (Fn::ImportValue) for IAM role ARNs exported by the IAM stack.
5. THE Pipeline template SHALL tag the EC2 instance-based Deployment_Group to match the existing EC2 Name tag pattern (`paga-{Environment}-ec2`).

### Requirement 7: Buildspec Files

**User Story:** As a developer, I want separate buildspec files for frontend and backend, so that each build process is self-documenting and independently maintainable.

#### Acceptance Criteria

1. THE Frontend_Build SHALL use the buildspec file at `infra/buildspec-frontend.yml`.
2. THE Backend_Build SHALL use the buildspec file at `infra/buildspec-backend.yml`.
3. THE Frontend_Build buildspec SHALL define install, build, and post_build phases corresponding to dependency install, production build, S3 sync, and CloudFront invalidation.
4. THE Backend_Build buildspec SHALL define install, pre_build, build, and post_build phases corresponding to SDK setup, restore/build, test, and publish/package.
5. THE Backend_Build buildspec SHALL declare the artifacts section mapping the published output, appspec.yml, scripts, and migration file to the expected artifact structure.

### Requirement 8: Security and Least Privilege

**User Story:** As a developer, I want the pipeline to follow least-privilege IAM principles, so that compromised credentials have minimal blast radius.

#### Acceptance Criteria

1. THE Frontend_Build SHALL operate with permissions limited to: writing to the frontend S3 bucket, creating CloudFront invalidations, reading/writing to the Artifact_Bucket, and writing CloudWatch Logs.
2. THE Backend_Build SHALL operate with permissions limited to: reading/writing to the Artifact_Bucket and writing CloudWatch Logs.
3. THE Pipeline SHALL operate with permissions limited to: starting CodeBuild builds, creating CodeDeploy deployments, and reading/writing to the Artifact_Bucket.
4. THE Pipeline template SHALL NOT contain any secrets, passwords, or tokens.
5. THE Backend_Deploy SHALL retrieve database credentials exclusively from SSM Parameter Store at deploy time, not from environment variables or template parameters.
