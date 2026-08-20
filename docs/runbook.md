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
