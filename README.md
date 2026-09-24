# Valetax Test

A small .NET microservice system for managing partner relationships, calculating
commissions, and crediting user wallets.

## Run with Docker

From the repository root:

```bash
docker compose up --build -d
```

Open the API Gateway Swagger UI at:

```text
http://localhost:5101/swagger
```

The Swagger document dropdown contains the PartnerGraph, Commissions, and Wallet
APIs. To check the stack or follow its logs:

```bash
docker compose ps
docker compose logs -f
```

Stop the project with `docker compose down`. Add `-v` only if you also want to
delete the local PostgreSQL and Kafka data.

## Architecture

- **API Gateway** is a YARP reverse proxy and provides one Swagger UI.
- **PartnerGraph** stores users and their partner hierarchy.
- **Commissions** processes profit events and calculates partner commissions.
- **Wallet** stores balances and applies commission payouts.
- Each stateful service owns its PostgreSQL database and applies its EF Core
  migrations at startup.
- Services communicate through REST, gRPC, and Kafka. A transactional outbox is
  used for reliable event publishing.

Docker Compose starts all services together with PostgreSQL and a single-node
Kafka broker.
