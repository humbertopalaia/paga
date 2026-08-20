# ADR 002 — Single-Instance Architecture

## Status

Accepted

## Context

The PAGA application (Palaia Acompanhamento de Gastos Automatizado) is a personal finance management web app entering its MVP/validation phase. The system consists of three runtime components:

- **.NET 10 API** (Kestrel on port 5000) serving the REST backend
- **PostgreSQL 16** database for persistent storage
- **Nginx** reverse proxy forwarding HTTP traffic to Kestrel

The MVP walking skeleton needs to run in production to validate the full deployment pipeline (CloudFormation, CodeDeploy, CloudFront) and gather early feedback. At this stage, user traffic is minimal (single-digit concurrent users), and operational cost must remain low.

We needed to decide how to host these components on AWS: separate managed services (RDS, ECS/Fargate, ALB) or co-located on a single EC2 instance.

## Decision

We will co-locate the .NET API, PostgreSQL, and Nginx on a **single EC2 t3.micro instance**. CloudFront serves the Angular SPA from S3 and proxies `/api/*` requests to the EC2 instance over HTTP port 80.

The architecture looks like:

```
CloudFront → S3 (SPA)
CloudFront → EC2 t3.micro (Nginx :80 → Kestrel :5000 → PostgreSQL :5432 localhost)
```

This decision is acceptable for the MVP/validation phase because:

1. The application has no SLA requirements or paying customers yet.
2. t3.micro costs ~$7.50/month, keeping the validation budget negligible.
3. It simplifies deployment — a single CodeDeploy target with one systemd service to manage.
4. It validates the full infrastructure pipeline (CloudFormation, CodeDeploy, Parameter Store) with minimal moving parts.

## Consequences

### Positive

- **Low cost**: a single t3.micro instance is the cheapest viable production setup.
- **Simplicity**: one machine to SSH into, monitor, and troubleshoot during early validation.
- **Fast iteration**: deploy scripts, Nginx config, and database migrations are all local to the instance.
- **Full pipeline validation**: proves CloudFormation stacks, CodeDeploy hooks, and Parameter Store integration work end-to-end.

### Negative / Trade-offs

- **Single point of failure**: if the instance crashes, both the API and database are unavailable. There is no automatic failover.
- **No horizontal scaling**: the API cannot scale independently of the database. Under unexpected load, the t3.micro CPU/memory budget is shared across all three processes.
- **No managed database**: PostgreSQL runs without automated backups, point-in-time recovery, or read replicas. Manual `pg_dump` is the only backup strategy.
- **Resource contention**: Nginx, Kestrel, and PostgreSQL share 1 GiB of RAM and 2 vCPUs (burstable). Memory pressure could cause OOM kills.
- **No zero-downtime deploys**: CodeDeploy stops Kestrel during deployment, causing brief downtime.

### Future Path

When the application moves beyond the MVP phase (real users, uptime requirements, or growing data), the architecture should evolve:

1. **Database**: migrate to Amazon RDS for PostgreSQL — automated backups, Multi-AZ failover, managed patching.
2. **Compute**: move the .NET API to ECS/Fargate or a multi-instance Auto Scaling Group behind an ALB, enabling horizontal scaling and zero-downtime rolling deploys.
3. **Separation of concerns**: each component gets its own scaling, monitoring, and failure domain.

This ADR will be superseded by a new decision record when the migration is planned.
