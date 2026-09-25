# Architecture

## System Architecture

```mermaid
flowchart LR
    Client[React client, future phase] --> Gateway[YARP API Gateway]
    Gateway --> Identity[Identity Service]
    Gateway --> Product[Product Service]
    Gateway --> Cart[Cart Service]
    Gateway --> Order[Order Service]
    Gateway --> Payment[Payment Service]
    Order -. events, future phase .-> Kafka[Kafka]
    Kafka -. events, future phase .-> Inventory[Inventory Service]
    Kafka -. events, future phase .-> Notification[Notification Service]
    Product -. future persistence .-> ProductDb[(Product database)]
    Order -. future persistence .-> OrderDb[(Order database)]
    Cart -. future storage .-> Redis[(Redis)]
```

## Phase 1 boundaries

Each service has its own ASP.NET Core host and project. Service databases and domain types will remain owned by the service that uses them; no cross-service database access or shared business model is introduced. The gateway handles routing only and does not own business behavior.

Compose currently provides shared local development dependencies: SQL Server, Redis, Kafka, and Kafka UI. It does not yet build or run the application hosts. Kafka runs as a single local KRaft broker for development, not as a production topology.