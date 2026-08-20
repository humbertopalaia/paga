# PAGA — Manual Deploy Runbook

Step-by-step instructions for provisioning the PAGA MVP infrastructure on AWS and deploying the application manually (without CI/CD).

---

## 1. Prerequisites

### Tools

- **AWS CLI v2** installed and configured
- **AWS profile `palaia`** configured with credentials that have admin access to the target account
- **.NET 10 SDK** installed locally (for `dotnet publish` and `dotnet ef`)
- **Node.js 20+** and **Angular CLI** installed locally (for `ng build`)
- **SSH key pair** created in AWS EC2 console (note the name for the `KeyPairName` parameter)

### Parameter Store Secrets

Create the following SecureString parameters in AWS SSM Parameter Store **before** creating any stack:

```powershell
aws ssm put-parameter `
  --name "/paga/jwt-key" `
  --type SecureString `
  --value "<your-jwt-signing-key-min-32-chars>" `
  --profile palaia

aws ssm put-parameter `
  --name "/paga/db-password" `
  --type SecureString `
  --value "<strong-database-password>" `
  --profile palaia

aws ssm put-parameter `
  --name "/paga/admin-password" `
  --type SecureString `
  --value "<admin-user-password>" `
  --profile palaia
```

> These secrets are read at runtime by the EC2 instance and deploy scripts. They must exist before the EC2 stack is created.

### Information You Need

| Item | Where to find it | Used in |
|------|-----------------|---------|
| Your public IP (CIDR) | `curl ifconfig.me` → append `/32` | Network stack `SshCidr` |
| EC2 key pair name | AWS Console → EC2 → Key Pairs | EC2 stack `KeyPairName` |
| Globally unique S3 bucket name | Choose one (e.g., `paga-prod-spa-<account-id>`) | Frontend stack `BucketName` |

---

## 2. Stack Creation

Stacks must be created **in this exact order** due to cross-stack export/import dependencies:

```
Network → IAM → EC2 → Frontend
```

### 2.1 Network Stack

```powershell
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

Wait for completion:

```powershell
aws cloudformation wait stack-create-complete --stack-name paga-prod-network --profile palaia
```

> The `CloudFrontPrefixListId` default (`pl-3b927c52`) is correct for **us-east-1**. Verify the prefix list ID for other regions.

### 2.2 IAM Stack

```powershell
aws cloudformation create-stack `
  --stack-name paga-prod-iam `
  --template-body file://infra/cloudformation/iam.yaml `
  --parameters `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=ArtifactBucketArn,ParameterValue=arn:aws:s3:::paga-prod-artifacts `
    ParameterKey=FrontendBucketArn,ParameterValue=arn:aws:s3:::<your-spa-bucket-name> `
    ParameterKey=CloudFrontDistributionArn,ParameterValue=arn:aws:cloudfront::<account-id>:distribution/<dist-id> `
  --capabilities CAPABILITY_NAMED_IAM `
  --profile palaia
```

Wait for completion:

```powershell
aws cloudformation wait stack-create-complete --stack-name paga-prod-iam --profile palaia
```

> **Note:** The `CloudFrontDistributionArn` is a chicken-and-egg situation on the first deploy. Options:
> 1. Use a wildcard ARN `arn:aws:cloudfront::<account-id>:distribution/*` for the initial creation, then update after the Frontend stack is created.
> 2. Create the artifact bucket first (`aws s3 mb s3://paga-prod-artifacts --profile palaia`).

### 2.3 EC2 Stack

```powershell
aws cloudformation create-stack `
  --stack-name paga-prod-ec2 `
  --template-body file://infra/cloudformation/ec2.yaml `
  --parameters `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=InstanceType,ParameterValue=t3.micro `
    ParameterKey=KeyPairName,ParameterValue=<your-key-pair-name> `
  --profile palaia
```

Wait for completion:

```powershell
aws cloudformation wait stack-create-complete --stack-name paga-prod-ec2 --profile palaia
```

Retrieve the EC2 public IP (needed for the Frontend stack):

```powershell
aws cloudformation describe-stacks `
  --stack-name paga-prod-ec2 `
  --query "Stacks[0].Outputs[?OutputKey=='Ec2PublicIp'].OutputValue" `
  --output text `
  --profile palaia
```

### 2.4 Frontend Stack

```powershell
aws cloudformation create-stack `
  --stack-name paga-prod-frontend `
  --template-body file://infra/cloudformation/frontend.yaml `
  --parameters `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=BucketName,ParameterValue=<your-spa-bucket-name> `
    ParameterKey=Ec2PublicIp,ParameterValue=<ec2-public-ip-from-previous-step> `
  --profile palaia
```

Wait for completion:

```powershell
aws cloudformation wait stack-create-complete --stack-name paga-prod-frontend --profile palaia
```

Retrieve the CloudFront domain:

```powershell
aws cloudformation describe-stacks `
  --stack-name paga-prod-frontend `
  --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDomain'].OutputValue" `
  --output text `
  --profile palaia
```

> After obtaining the CloudFront domain, update `backend/src/Paga.Api/appsettings.Production.json` with the actual `https://<domain>.cloudfront.net` origin in `Cors:AllowedOrigins`.

---

## 3. Backend Deploy

### 3.1 Publish the API

From the repository root:

```powershell
dotnet publish backend/src/Paga.Api -c Release -o ./publish
```

### 3.2 Generate the Idempotent Migration Script

```powershell
dotnet ef migrations script `
  --idempotent `
  --project backend/src/Paga.Infrastructure `
  --startup-project backend/src/Paga.Api `
  --output ./publish/migrations.sql
```

### 3.3 Transfer Files to EC2

```powershell
scp -i <path-to-private-key.pem> -r ./publish/* ec2-user@<ec2-public-ip>:/opt/paga/api/
```

### 3.4 Run the Migration on EC2

SSH into the instance:

```powershell
ssh -i <path-to-private-key.pem> ec2-user@<ec2-public-ip>
```

On the EC2 instance:

```bash
# Read the DB password from Parameter Store
DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)

# Run the idempotent migration
psql "postgresql://paga:${DB_PASSWORD}@localhost:5432/paga" -f /opt/paga/api/migrations.sql
```

### 3.5 Configure Environment Variables

Create or update the environment file for the systemd service:

```bash
# Read secrets from Parameter Store
JWT_KEY=$(aws ssm get-parameter --name /paga/jwt-key --with-decryption --query Parameter.Value --output text)
DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)
ADMIN_PASSWORD=$(aws ssm get-parameter --name /paga/admin-password --with-decryption --query Parameter.Value --output text)

# Write environment file for systemd
sudo tee /opt/paga/api/.env > /dev/null <<EOF
ConnectionStrings__Default=Host=localhost;Port=5432;Database=paga;Username=paga;Password=${DB_PASSWORD}
Jwt__Key=${JWT_KEY}
Seed__AdminEmail=palaia@increvasenocanal.com
Seed__AdminPassword=${ADMIN_PASSWORD}
Cors__AllowedOrigins__0=https://<cloudfront-domain>.cloudfront.net
EOF

sudo chmod 600 /opt/paga/api/.env
```

> Replace `<cloudfront-domain>` with the actual CloudFront distribution domain from step 2.4.

### 3.6 Restart the Service

```bash
sudo systemctl restart paga-api.service
sudo systemctl status paga-api.service
```

Check logs if the service fails to start:

```bash
sudo journalctl -u paga-api.service -n 50 --no-pager
```

---

## 4. Frontend Deploy

### 4.1 Build the Angular SPA

From the repository root:

```powershell
cd frontend
ng build --configuration production
```

The output is in `frontend/dist/frontend/browser/` (Angular 19 default output path).

### 4.2 Sync to S3

```powershell
aws s3 sync frontend/dist/frontend/browser/ s3://<your-spa-bucket-name>/ `
  --delete `
  --profile palaia
```

### 4.3 Invalidate CloudFront Cache

Retrieve the distribution ID:

```powershell
$DIST_ID = aws cloudformation describe-stacks `
  --stack-name paga-prod-frontend `
  --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionId'].OutputValue" `
  --output text `
  --profile palaia
```

Create the invalidation:

```powershell
aws cloudfront create-invalidation `
  --distribution-id $DIST_ID `
  --paths "/*" `
  --profile palaia
```

> Invalidation takes 1-5 minutes to propagate globally.

---

## 5. Validation

Run these checks after deploying both backend and frontend.

### 5.1 Health Check via CloudFront

```powershell
curl -s https://<cloudfront-domain>.cloudfront.net/api/health
```

Expected response:

```json
{"status":"Healthy"}
```

### 5.2 Health Check Directly on EC2 (via SSH)

```bash
curl -sf http://localhost:5000/health
```

### 5.3 Load the SPA

Open `https://<cloudfront-domain>.cloudfront.net` in a browser. The Angular login page should render.

### 5.4 Test API Call (Login)

```powershell
curl -X POST https://<cloudfront-domain>.cloudfront.net/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{"email":"palaia@increvasenocanal.com","password":"<admin-password>"}'
```

Expected: 200 with `TokenResponse` containing `accessToken`, `refreshToken`, `expiresIn`.

### 5.5 Verify CORS Headers

```powershell
curl -I -X OPTIONS https://<cloudfront-domain>.cloudfront.net/api/auth/login `
  -H "Origin: https://<cloudfront-domain>.cloudfront.net" `
  -H "Access-Control-Request-Method: POST"
```

The response should include `Access-Control-Allow-Origin: https://<cloudfront-domain>.cloudfront.net`.

---

## 6. Troubleshooting

### Security Group Blocking Traffic

**Symptom:** CloudFront returns 502/504 errors; direct EC2 access on port 80 times out.

**Diagnosis:**

```powershell
aws ec2 describe-security-groups `
  --filters "Name=group-name,Values=paga-prod-sg" `
  --query "SecurityGroups[0].IpPermissions" `
  --profile palaia
```

**Common causes:**
- Prefix list `pl-3b927c52` not found in the region (verify the correct prefix list ID for your region)
- Security group was not associated with the instance
- Port 80 rule is missing or restricted to wrong source

**Fix:** Update the Network stack with the correct `CloudFrontPrefixListId` and re-deploy.

---

### Nginx Misconfiguration

**Symptom:** Port 80 is reachable but returns 502 Bad Gateway or Nginx default page.

**Diagnosis (on EC2):**

```bash
# Check Nginx is running
sudo systemctl status nginx

# Check Nginx configuration
sudo nginx -t

# Check the paga.conf is linked/included
ls -la /etc/nginx/conf.d/paga.conf

# Check Nginx error log
sudo tail -20 /var/log/nginx/error.log
```

**Common causes:**
- `paga.conf` not placed in `/etc/nginx/conf.d/`
- Default Nginx config in `/etc/nginx/nginx.conf` has a conflicting `server` block on port 80
- Kestrel is not running on port 5000 (Nginx returns 502)

**Fix:**

```bash
# Ensure paga.conf is in place
sudo cp /opt/paga/api/infra/nginx/paga.conf /etc/nginx/conf.d/paga.conf

# Remove default server block if conflicting
sudo sed -i '/^[^#]*server {/,/^[^#]*}/d' /etc/nginx/nginx.conf

# Reload
sudo nginx -t && sudo systemctl reload nginx
```

---

### Kestrel Not Starting

**Symptom:** `systemctl status paga-api.service` shows `failed` or `inactive`.

**Diagnosis (on EC2):**

```bash
# Check service status
sudo systemctl status paga-api.service

# Check recent logs
sudo journalctl -u paga-api.service -n 100 --no-pager

# Verify the DLL exists
ls -la /opt/paga/api/Paga.Api.dll

# Check environment file
sudo cat /opt/paga/api/.env
```

**Common causes:**
- Missing `.env` file or incorrect environment variables
- .NET runtime not installed (`dotnet --info` to verify)
- Missing `Paga.Api.dll` (publish step failed or scp was incomplete)
- Wrong `ASPNETCORE_URLS` — must be `http://localhost:5000`
- Database connection string incorrect (Kestrel fails during startup seed)

**Fix:**

```bash
# Re-read secrets and regenerate .env (see section 3.5)
# Then restart
sudo systemctl restart paga-api.service
sudo journalctl -u paga-api.service -f
```

---

### Database Connection Failure

**Symptom:** `/health` returns 503 or Kestrel logs show `Npgsql.NpgsqlException: Failed to connect`.

**Diagnosis (on EC2):**

```bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Check PostgreSQL is listening on localhost
sudo ss -tlnp | grep 5432

# Test connection manually
DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)
psql "postgresql://paga:${DB_PASSWORD}@localhost:5432/paga" -c "SELECT 1;"

# Check pg_hba.conf allows local connections
sudo cat /var/lib/pgsql/data/pg_hba.conf | grep -v "^#"
```

**Common causes:**
- PostgreSQL service not running or not enabled on boot
- Database `paga` or user `paga` not created (user data script failed)
- Password in Parameter Store doesn't match what was set during EC2 provisioning
- `listen_addresses` not set to `localhost` in `postgresql.conf`

**Fix:**

```bash
# Start PostgreSQL if stopped
sudo systemctl start postgresql
sudo systemctl enable postgresql

# If database/user is missing, recreate
sudo -u postgres psql -c "CREATE USER paga WITH PASSWORD '<password>';"
sudo -u postgres psql -c "CREATE DATABASE paga OWNER paga;"

# Restart the API after fixing
sudo systemctl restart paga-api.service
```

---

### CloudFront Returning Stale Content

**Symptom:** Frontend shows old version after deploying new files to S3.

**Fix:**

```powershell
aws cloudfront create-invalidation `
  --distribution-id <distribution-id> `
  --paths "/*" `
  --profile palaia
```

Wait 1-5 minutes for propagation.

---

### Migration Script Fails

**Symptom:** `psql` returns errors when running `migrations.sql`.

**Diagnosis (on EC2):**

```bash
DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)
psql "postgresql://paga:${DB_PASSWORD}@localhost:5432/paga" -f /opt/paga/api/migrations.sql
```

**Common causes:**
- Migration script was not included in the publish output (check `./publish/migrations.sql`)
- Database user lacks permissions (should be owner of the `paga` database)
- Script is not idempotent (regenerate with `--idempotent` flag)

**Fix:** Regenerate the migration script locally and re-transfer:

```powershell
dotnet ef migrations script `
  --idempotent `
  --project backend/src/Paga.Infrastructure `
  --startup-project backend/src/Paga.Api `
  --output ./publish/migrations.sql

scp -i <key.pem> ./publish/migrations.sql ec2-user@<ec2-ip>:/opt/paga/api/migrations.sql
```


---

## 7. CI/CD Pipeline Deployment

This section documents how to deploy the automated CI/CD pipeline that replaces the manual deploy process in sections 3 and 4. Once the pipeline is active, every push to `main` will automatically build, test, and deploy both frontend and backend.

### 7.1 Prerequisites

Before deploying the pipeline stack, ensure:

1. **Stacks 2.1 through 2.4 are already deployed** (Network, IAM, EC2, Frontend). The pipeline depends on cross-stack exports from IAM and uses existing infrastructure.

2. **CodeDeploy agent is installed on the EC2 instance** — the EC2 user data script should have installed it. Verify via SSH:

   ```bash
   sudo service codedeploy-agent status
   ```

3. **Gather the following values** (needed as parameters):

   | Value | How to obtain |
   |-------|---------------|
   | Frontend S3 bucket name | From Frontend stack output or the `BucketName` parameter used in step 2.4 |
   | CloudFront distribution ID | `aws cloudformation describe-stacks --stack-name paga-prod-frontend --query "Stacks[0].Outputs[?OutputKey=='CloudFrontDistributionId'].OutputValue" --output text --profile palaia` |
   | EC2 instance Name tag | `paga-prod-ec2` (matches the EC2 stack) |
   | AWS Account ID | `aws sts get-caller-identity --query Account --output text --profile palaia` |

### 7.2 Create the CodeStar Connection (one-time)

The CodeStar Connection links AWS CodePipeline to the GitHub repository. This is a **one-time manual step** that requires OAuth consent in the AWS Console.

1. Open the AWS Console → **Developer Tools → Settings → Connections**
2. Click **Create connection**
3. Select **GitHub** as the provider
4. Name it `paga-github-connection` (or any descriptive name)
5. Click **Connect to GitHub** — this opens a GitHub OAuth window
6. Authorize the AWS Connector app for the `humbertopalaia` account (or the org that owns the repo)
7. Select the repository `paga` (or grant access to all repos)
8. Click **Connect**

Once created, copy the **Connection ARN** (format: `arn:aws:codeconnections:<region>:<account-id>:connection/<uuid>`). You'll need it in step 7.4.

> **Note:** The connection starts in "Pending" status until you complete the GitHub handshake. It must show "Available" before the pipeline can use it.

### 7.3 Update the IAM Stack

The IAM stack has been updated in code to export `CodePipelineRoleArn` and `CodeBuildRoleArn`, and includes the `CodeStarConnectionAccess` permission. You must update the deployed stack to reflect these changes.

The IAM stack now requires an `ArtifactBucketArn` parameter. Choose a globally unique bucket name for pipeline artifacts (e.g., `paga-prod-artifacts-<account-id>`) — the bucket itself is created by the pipeline stack, but the IAM stack needs the ARN to scope permissions.

```powershell
aws cloudformation update-stack `
  --stack-name paga-prod-iam `
  --template-body file://infra/cloudformation/iam.yaml `
  --parameters `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=ArtifactBucketArn,ParameterValue=arn:aws:s3:::paga-prod-artifacts `
    ParameterKey=FrontendBucketArn,ParameterValue=arn:aws:s3:::<your-spa-bucket-name> `
    ParameterKey=CloudFrontDistributionArn,ParameterValue=arn:aws:cloudfront::<account-id>:distribution/<dist-id> `
  --capabilities CAPABILITY_NAMED_IAM `
  --profile palaia
```

Wait for completion:

```powershell
aws cloudformation wait stack-update-complete --stack-name paga-prod-iam --profile palaia
```

> **Important:** The `ArtifactBucketArn` value (`arn:aws:s3:::paga-prod-artifacts`) must match the `ArtifactBucketName` parameter you'll pass to the pipeline stack in the next step. Only the bucket name portion needs to match.

### 7.4 Deploy the Pipeline Stack

Create the pipeline stack with all required parameters:

```powershell
aws cloudformation create-stack `
  --stack-name paga-prod-pipeline `
  --template-body file://infra/cloudformation/pipeline.yaml `
  --parameters `
    ParameterKey=Environment,ParameterValue=prod `
    ParameterKey=GitHubOwner,ParameterValue=humbertopalaia `
    ParameterKey=GitHubRepo,ParameterValue=paga `
    ParameterKey=GitHubBranch,ParameterValue=main `
    ParameterKey=CodeStarConnectionArn,ParameterValue=<connection-arn-from-step-7.2> `
    ParameterKey=ArtifactBucketName,ParameterValue=paga-prod-artifacts `
    ParameterKey=FrontendBucketName,ParameterValue=<your-spa-bucket-name> `
    ParameterKey=CloudFrontDistributionId,ParameterValue=<cloudfront-dist-id> `
    ParameterKey=Ec2TagValue,ParameterValue=paga-prod-ec2 `
  --profile palaia
```

Wait for completion:

```powershell
aws cloudformation wait stack-create-complete --stack-name paga-prod-pipeline --profile palaia
```

### 7.5 Trigger the First Pipeline Execution

The pipeline triggers automatically on pushes to `main`. To run it for the first time:

- **Option A:** Push any commit to `main` (e.g., merge a branch).
- **Option B:** Manually start the pipeline in the AWS Console → CodePipeline → `paga-prod-pipeline` → **Release change**.

### 7.6 Verify the Pipeline

Monitor the pipeline execution:

```powershell
aws codepipeline get-pipeline-state `
  --name paga-prod-pipeline `
  --profile palaia
```

Check individual stage results:

1. **Source** — should show "Succeeded" after fetching from GitHub.
2. **Build** — both `FrontendBuild` and `BackendBuild` run in parallel. Check logs:

   ```powershell
   aws codebuild list-builds-for-project `
     --project-name paga-prod-frontend-build `
     --profile palaia

   aws codebuild list-builds-for-project `
     --project-name paga-prod-backend-build `
     --profile palaia
   ```

3. **Deploy** — CodeDeploy installs the backend on EC2. Check deployment status:

   ```powershell
   aws deploy list-deployments `
     --application-name paga-prod-app `
     --deployment-group-name paga-prod-deployment-group `
     --profile palaia
   ```

After the pipeline completes, run the validation checks from section 5 (health check, SPA load, login).

### 7.7 Pipeline Troubleshooting

#### Source stage fails

**Common causes:**
- CodeStar Connection is in "Pending" state (complete the GitHub OAuth flow in Console)
- Connection ARN is incorrect
- Repository name or owner doesn't match

**Fix:** Verify the connection status in AWS Console → Developer Tools → Connections.

#### Frontend build fails

**Common causes:**
- `npm ci` fails due to lockfile mismatch (push an updated `package-lock.json`)
- `ng build` fails due to compilation errors (fix and push)
- S3 sync fails due to IAM permissions (check CodeBuild role has `FrontendBucketSync` permissions)

**Diagnosis:**

```powershell
aws codebuild batch-get-builds `
  --ids <build-id> `
  --query "builds[0].logs.deepLink" `
  --output text `
  --profile palaia
```

Open the deep link to view full build logs in CloudWatch.

#### Backend build fails

**Common causes:**
- `dotnet test` fails (fix tests and push)
- `dotnet ef` tool not installed (check the install phase in `infra/buildspec-backend.yml`)
- Migration script generation fails (EF context issue)

#### Deploy fails

**Common causes:**
- CodeDeploy agent not running on EC2 (`sudo service codedeploy-agent restart`)
- Migration script fails (check DB credentials in SSM)
- Service fails health check within 30s (check Kestrel logs on EC2)

**Diagnosis (on EC2):**

```bash
# Check CodeDeploy agent logs
sudo tail -50 /var/log/aws/codedeploy-agent/codedeploy-agent.log

# Check deployment lifecycle event logs
sudo tail -50 /opt/codedeploy-agent/deployment-root/deployment-logs/codedeploy-agent-deployments.log
```

---

> **After the pipeline is active**, sections 3 and 4 (manual backend/frontend deploy) are no longer needed for regular deployments. They remain as a reference for emergency manual deploys or initial setup.
