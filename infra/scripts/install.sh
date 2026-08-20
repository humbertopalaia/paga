#!/bin/bash
set -e

# Read database password from SSM Parameter Store (using EC2 instance profile)
DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)

# Execute idempotent migration script (files already copied by CodeDeploy Install step)
PGHOST=localhost PGPORT=5432 PGDATABASE=paga PGUSER=paga PGPASSWORD="$DB_PASSWORD" psql -f /opt/paga/api/migrations.sql