#!/bin/bash
set -e

# Read database password from SSM Parameter Store (using EC2 instance profile)
DB_PASSWORD=$(aws ssm get-parameter --name /paga/db-password --with-decryption --query Parameter.Value --output text)

# Construct psql connection string for localhost
CONN_STRING="postgresql://paga:${DB_PASSWORD}@localhost:5432/paga"

# Execute idempotent migration script
psql "$CONN_STRING" -f /opt/paga/api/migrations.sql
