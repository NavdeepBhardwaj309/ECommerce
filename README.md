# Event-Driven E-Commerce Platform

This repository is being built incrementally. Phase 1 provides the .NET solution, independently buildable API hosts, YARP gateway, health probes, and local infrastructure dependencies. Business APIs and application containers are added in later phases.

## Phase 1

- `ECommerce.sln` contains the API Gateway and the seven service hosts.
- Each host exposes `/health`, `/live`, and `/ready`.
- YARP routes identity, products, cart, orders, and payments to local service ports.
- Docker Compose runs SQL Server, Redis, single-node Kafka in KRaft mode, and Kafka UI.
- Local dependency data is persisted in named Docker volumes.

See [local development](docs/local-development.md) to start the dependencies and run the hosts, and [architecture](docs/architecture.md) for service ownership and communication boundaries.

## Current scope

This is Phase 1 only. The health endpoints are infrastructure probes, not business APIs. Authentication, database schemas, event processing, containers for application services, and Kubernetes manifests are intentionally deferred to their implementation phases.