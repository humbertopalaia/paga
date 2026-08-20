# Implementation Plan: AWS Deploy Checklist

## Overview

Operational checklist for provisioning and deploying the PAGA MVP on AWS. This is a sequential execution plan — not application code. Each task is a self-contained step with exact commands, execution context annotations (`[LOCAL]` for PowerShell, `[EC2]` for Bash), and validation gates.

The Operator fills in the variables table first, then executes phases 1–5 in strict order.

## Tasks

- [x] 1. Phase 1: Prerequisites Verification [LOCAL]
  - Verify all tools, credentials, and secrets before proceeding
  - Fill in the Variables table with operator-specific values
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8_

  - [x] 1.1 Record operator variables
    - Fill in the following values that will be used throughout the checklist:
    - `<your-ip>/32` — your public IP in CIDR notation (run `curl ifconfig.me` and append `/32`)
    - `<your-key-pair-name>` — EC2 key pair name from AWS Console → EC2 → Key Pairs
    - `<your-spa-bucket-name>` — globally unique S3 bucket name (e.g., `paga-prod-spa-<account-id>`)
    - `<path-to-private-key.pem>` — local path to the private key file for the key pair
    - **Success:** All four values recorded
    - _Requirements: 1.7_

  - [x] 1.2 Verify AWS CLI and profile [LOCAL]
    - ```powershell
      aws --version
      ```
    - **Expected:** `aws-cli/2.x.x ...`
    - ```powershell
      aws sts get-caller-identity --profile palaia
      ```
    - **Expected:** JSON with `Account`, `UserId`, `Arn` — record the `Account` value as `<account-id>`
    - **Error:** If command fails, run `aws configure --profile palaia` and enter credentials
    - _Requirements: 1.1, 1.2, 1.8_

  - [x] 1.3 Verify .NET SDK [LOCAL]
    - ```powershell
      dotnet --version
      ```
    - **Expected:** `10.x.x`
    - **Error:** If not installed, download from https://dotnet.microsoft.com/download
    - _Requirements: 1.3_

  - [x] 1.4 Verify Node.js and Angular CLI [LOCAL]
    - ```powershell
      node --version
      ```
    - **Expected:** `v20.x.x` or higher
    - ```powershell
      ng version
      ```
    - **Expected:** Angular CLI version output (19.x)
    - **Error:** If missing, install Node.js from https://nodejs.org and run `npm install -g @angular/cli`
    - _Requirements: 1.4, 1.5_

  - [x] 1.5 Create Parameter Store secrets [LOCAL]
    - ```powershell
      aws ssm put-parameter `
        --name "/paga/jwt-key" `
        --type SecureString `
        --value "<your-jwt-signing-key-min-32-chars>" `
        --profile palaia
      ```
    - ```powershell
      aws ssm put-parameter `
        --name "/paga/db-password" `
        --type SecureString `
        --value "<strong-database-password>" `
        --profile palaia
      ```
    - ```powershell
      aws ssm put-parameter `
        --name "/paga/admin-password" `
        --type SecureString `
        --value "<admin-user-password>" `
        --profile palaia
      ```
    - **Expected:** Each command returns `{ "Version": 1, "Tier": "Standard" }`
    - **Error:** If parameter already exists, the command fails with `ParameterAlreadyExists` — this is expected on re-deploys; skip
    - _Requirements: 1.6, 1.8_

- [x] 2. Phase 2: Infrastructure Provisioning [LOCAL]
  - Create CloudFormation stacks in dependency order: Network → IAM → EC2 → Frontend
  - Each stack must complete before the next begins
  - _Requirements: 2.1, 3.1, 4.1, 5.1_

  - [x] 2.1 Create Network Stack [LOCAL]
    - ```powershell
      aws cloudformation create-stack `
        --stack-name paga-prod-network `
        --template-body file://infra/cloudformation/network.yaml `
        --parameters `
          ParameterKey=Environment,ParameterValue=prod `
          ParameterKey=VpcCidr,ParameterValue=10.0.0.0/16 `
          ParameterKey=SshCidr,ParameterValue=<your-ip>/32 `
          ParameterKey=CloudFrontPrefixListId,ParameterValue=pl-3b927c52 `
        --profile palaia
      ```
    - ```powershell
      aws cloudformation wait stack-create-complete --stack-name paga-prod-network --profile palaia
      ```
    - **Expected:** Wait command returns with exit code 0 (no output)
    - **Error:** If wait fails, diagnose with:
      ```powershell
      aws cloudformation describe-stack-events `
        --stack-name paga-prod-network `
        --query "StackEvents[?ResourceStatus=='CREATE_FAILED']" `
        --profile palaia
      ```
      Fix the issue, delete the stack (`aws cloudformation delete-stack --stack-name paga-prod-network --profile palaia`), wait for deletion, and retry
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 2.2 Create IAM Stack [LOCAL]
    - ```powershell
      aws cloudformation create-stack `
        --stack-name paga-prod-iam `
        --template-body file://infra/cloudformation/iam.yaml `
        --parameters `
          ParameterKey=Environment,ParameterValue=prod `
          ParameterKey=ArtifactBucketArn,ParameterValue=arn:aws:s3:::paga-prod-artifacts `
          ParameterKey=FrontendBucketArn,ParameterValue=arn:aws:s3:::<your-spa-bucket-name> `
          ParameterKey=CloudFrontDistributionArn,ParameterValue=arn:aws:cloudfront::<account-id>:distribution/* `
        --capabilities CAPABILITY_NAMED_IAM `
        --profile palaia
      ```
    - > **Note:** Using wildcard `distribution/*` because the Frontend stack (and its distribution) doesn't exist yet. This is safe for the first deploy.
    - ```powershell
      aws cloudformation wait stack-create-complete --stack-name paga-prod-iam --profile palaia
      ```
    - **Expected:** Wait command returns with exit code 0
    - **Error:** Same diagnosis pattern — `describe-stack-events`, fix, delete, retry
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 2.3 Create EC2 Stack [LOCAL]
    - ```powershell
      aws cloudformation create-stack `
        --stack-name paga-prod-ec2 `
        --template-body file://infra/cloudformation/ec2.yaml `
        --parameters `
          ParameterKey=Environment,ParameterValue=prod `
          ParameterKey=InstanceType,ParameterValue=t3.micro `
          ParameterKey=KeyPairName,ParameterValue=<your-key-pair-name> `
        --profile palaia
      ```
    - ```powershell
      aws cloudformation wait stack-create-complete --stack-name paga-prod-ec2 --profile palaia
      ```
    - **Capture the EC2 public IP:**
      ```powershell
      aws cloudformation describe-stacks `
        --stack-name paga-prod-ec2 `
        --query "Stacks[0].Outputs[?OutputKey=='Ec2PublicIp'].OutputValue" `
        --output text `
        --profile palaia
      ```
    - **Expected:** An IP address (e.g., `54.123.45.67`) — record this as `<ec2-public-ip>`
    - **Error:** Same diagnosis pattern — `describe-stack-events`, fix, delete, retry
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 2.4 Create Frontend Stack [LOCAL]
    - ```powershell
      aws cloudformation create-stack `
        --stack-name paga-prod-frontend `
        --template-body file://infra/cloudformation/frontend.yaml `
        --parameters `
          ParameterKey=Environment,ParameterValue=prod `
          ParameterKey=BucketName,ParameterValue=<your-spa-bucket-name> `
          ParameterKey=Ec2PublicIp,ParameterValue=<ec2-public-ip> `
        --profile palaia
      ```
    - ```powershell
      aws cloudformation wait stack-create-complete --stack-name paga-prod-frontend --profile palaia
      ```
    - **Capture the CloudFront domain:**
      ```powershell
      aws cloudformation describe-stacks `
        --stack-name paga-prod-frontend `
        --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDomain'].OutputValue" `
        --output text `
        --profile palaia
      ```
    - **Capture the CloudFront distribution ID:**
      ```powershell
      aws cloudformation describe-stacks `
        --stack-name paga-prod-frontend `
        --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionId'].OutputValue" `
        --output text `
        --profile palaia
      ```
    - **Expected:** A domain like `d1234abcdef.cloudfront.net` — record as `<cloudfront-domain>`; a distribution ID like `E1234ABCDEF` — record as `<distribution-id>`
    - **Error:** Same diagnosis pattern — `describe-stack-events`, fix, delete, retry
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 3. Checkpoint — Infrastructure complete
  - All four stacks created successfully. Verify you have recorded:
    - `<account-id>` from step 1.2
    - `<ec2-public-ip>` from step 2.3
    - `<cloudfront-domain>` from step 2.4
    - `<distribution-id>` from step 2.4
  - Ensure all values are filled before proceeding to backend deploy.

- [x] 4. Phase 3: Backend Deploy [LOCAL → EC2]
  - Publish, transfer, migrate, configure, and start the API
  - _Requirements: 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 8.1, 8.2, 8.3, 8.4, 8.5_

  - [x] 4.1 Publish the .NET API [LOCAL]
    - Run from the repository root:
      ```powershell
      dotnet publish backend/src/Paga.Api -c Release -o ./publish
      ```
    - **Expected:** `Paga.Api -> ...\publish\` with no errors
    - _Requirements: 6.1_

  - [x] 4.2 Generate idempotent migration script [LOCAL]
    - ```powershell
      dotnet ef migrations script `
        --idempotent `
        --project backend/src/Paga.Infrastructure `
        --startup-project backend/src/Paga.Api `
        --output ./publish/migrations.sql
      ```
    - **Expected:** File `./publish/migrations.sql` created
    - _Requirements: 6.2_

  - [x] 4.3 Transfer files to EC2 [LOCAL]
    - ```powershell
      scp -i <path-to-private-key.pem> -r ./publish/* ec2-user@<ec2-public-ip>:/opt/paga/api/
      ```
    - **Expected:** Files transferred without errors
    - **Error:** If `Connection refused`, verify EC2 instance is running and security group allows SSH from your IP
    - _Requirements: 6.3_

  - [x] 4.4 SSH into EC2 and run migration [EC2]
    - Open SSH session:
      ```powershell
      ssh -i <path-to-private-key.pem> ec2-user@<ec2-public-ip>
      ```
    - Once connected (prompt changes to EC2), run:
      ```bash
      DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)
      psql "postgresql://paga:${DB_PASSWORD}@localhost:5432/paga" -f /opt/paga/api/migrations.sql
      ```
    - **Expected:** SQL statements execute with no errors; tables are created or already exist (idempotent)
    - **Error:** If `psql: FATAL: password authentication failed`, the password in Parameter Store doesn't match what was set during EC2 provisioning. Verify with `sudo -u postgres psql -c "\du"`
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 4.5 Configure environment variables [EC2]
    - Still inside the SSH session:
      ```bash
      JWT_KEY=$(aws ssm get-parameter --name /paga/jwt-key --with-decryption --query Parameter.Value --output text)
      DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)
      ADMIN_PASSWORD=$(aws ssm get-parameter --name /paga/admin-password --with-decryption --query Parameter.Value --output text)

      sudo tee /opt/paga/api/.env > /dev/null <<EOF
      ConnectionStrings__Default=Host=localhost;Port=5432;Database=paga;Username=paga;Password=${DB_PASSWORD}
      Jwt__Key=${JWT_KEY}
      Seed__AdminEmail=palaia@increvasenocanal.com
      Seed__AdminPassword=${ADMIN_PASSWORD}
      Cors__AllowedOrigins__0=https://<cloudfront-domain>.cloudfront.net
      EOF

      sudo chmod 600 /opt/paga/api/.env
      ```
    - > **Important:** Replace `<cloudfront-domain>` with the actual domain captured in step 2.4 (e.g., `d1234abcdef`)
    - **Expected:** File created with permissions `-rw-------`
    - _Requirements: 8.1, 8.2, 8.3_

  - [x] 4.6 Start and verify the API service [EC2]
    - ```bash
      sudo systemctl restart paga-api.service
      sudo systemctl status paga-api.service
      ```
    - **Expected:** Status shows `Active: active (running)`
    - Quick health check from EC2:
      ```bash
      curl -sf http://localhost:5000/health
      ```
    - **Expected:** `{"status":"Healthy"}`
    - **Error:** If service fails, inspect logs:
      ```bash
      sudo journalctl -u paga-api.service -n 50 --no-pager
      ```
      Common causes: missing `.env`, wrong connection string, .NET runtime not installed
    - Exit the SSH session when done:
      ```bash
      exit
      ```
    - _Requirements: 8.4, 8.5_

- [x] 5. Phase 4: Frontend Deploy [LOCAL]
  - Build the Angular SPA, sync to S3, and invalidate CloudFront cache
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 5.1 Build the Angular SPA [LOCAL]
    - From the repository root:
      ```powershell
      cd frontend
      ng build --configuration production
      cd ..
      ```
    - **Expected:** Build completes with no errors; output in `frontend/dist/frontend/browser/`
    - _Requirements: 9.1_

  - [x] 5.2 Sync to S3 [LOCAL]
    - ```powershell
      aws s3 sync frontend/dist/frontend/browser/ s3://<your-spa-bucket-name>/ `
        --delete `
        --profile palaia
      ```
    - **Expected:** Files uploaded, stale files removed; output lists upload/delete operations
    - _Requirements: 9.2_

  - [x] 5.3 Invalidate CloudFront cache [LOCAL]
    - ```powershell
      aws cloudfront create-invalidation `
        --distribution-id <distribution-id> `
        --paths "/*" `
        --profile palaia
      ```
    - **Expected:** JSON response with `Invalidation.Status = "InProgress"`
    - > Cache invalidation takes 1–5 minutes to propagate globally. Wait before validation.
    - _Requirements: 9.3, 9.4_

- [x] 6. Phase 5: End-to-End Validation [LOCAL]
  - Verify the full deployment is operational
  - All gates must pass before declaring the deploy complete
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [x] 6.1 Health check via CloudFront [LOCAL]
    - ```powershell
      curl -s https://<cloudfront-domain>.cloudfront.net/api/health
      ```
    - **Expected:** `{"status":"Healthy"}`
    - **Error:** If 502/504, check Nginx config on EC2; if 403, check CloudFront origin configuration
    - _Requirements: 10.1_

  - [x] 6.2 Load the SPA in browser [LOCAL]
    - Open `https://<cloudfront-domain>.cloudfront.net` in a browser
    - **Expected:** Angular login page renders correctly
    - **Error:** If blank page or 403, verify S3 sync completed and CloudFront invalidation propagated
    - _Requirements: 10.2_

  - [x] 6.3 Test login API [LOCAL]
    - ```powershell
      curl -s -X POST https://<cloudfront-domain>.cloudfront.net/api/auth/login `
        -H "Content-Type: application/json" `
        -d '{"email":"palaia@increvasenocanal.com","password":"<admin-password>"}'
      ```
    - **Expected:** HTTP 200 with JSON containing `accessToken`, `refreshToken`, `expiresIn`
    - **Error:** If 401, verify admin was seeded — check `Seed__AdminEmail` and `Seed__AdminPassword` in `.env` on EC2
    - _Requirements: 10.3_

  - [x] 6.4 Verify CORS headers [LOCAL]
    - ```powershell
      curl -I -X OPTIONS https://<cloudfront-domain>.cloudfront.net/api/auth/login `
        -H "Origin: https://<cloudfront-domain>.cloudfront.net" `
        -H "Access-Control-Request-Method: POST"
      ```
    - **Expected:** Response includes `Access-Control-Allow-Origin: https://<cloudfront-domain>.cloudfront.net`
    - **Error:** If header missing, verify `Cors__AllowedOrigins__0` in `.env` matches the exact CloudFront URL (including `https://`)
    - _Requirements: 10.4_

- [x] 7. Deployment Complete
  - All validation gates passed — the PAGA MVP is operational on AWS.
  - CloudFront URL: `https://<cloudfront-domain>.cloudfront.net`
  - Admin login: `palaia@increvasenocanal.com`
  - _Requirements: 10.5_

## Notes

- This is an operational checklist, not a code-generation task list. Each step is executed manually by the Operator.
- All `aws` commands use `--profile palaia` — never the default profile.
- Secrets flow from Parameter Store to shell variables to `.env` — they are never echoed or stored in command history.
- The checklist is designed for first-deploy. Re-deploys skip secret creation and use `update-stack` instead of `create-stack`.
- If any phase fails, fix the issue before proceeding — do not skip steps.
- The `CloudFrontPrefixListId` default (`pl-3b927c52`) is correct for `us-east-1`. Verify for other regions.
- Tasks marked with `[EC2]` execute within an SSH session. Always `exit` before returning to `[LOCAL]` commands.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4"] },
    { "id": 1, "tasks": ["1.5"] },
    { "id": 2, "tasks": ["2.1"] },
    { "id": 3, "tasks": ["2.2"] },
    { "id": 4, "tasks": ["2.3"] },
    { "id": 5, "tasks": ["2.4"] },
    { "id": 6, "tasks": ["4.1", "4.2"] },
    { "id": 7, "tasks": ["4.3"] },
    { "id": 8, "tasks": ["4.4"] },
    { "id": 9, "tasks": ["4.5"] },
    { "id": 10, "tasks": ["4.6"] },
    { "id": 11, "tasks": ["5.1"] },
    { "id": 12, "tasks": ["5.2"] },
    { "id": 13, "tasks": ["5.3"] },
    { "id": 14, "tasks": ["6.1", "6.2"] },
    { "id": 15, "tasks": ["6.3"] },
    { "id": 16, "tasks": ["6.4"] }
  ]
}
```
