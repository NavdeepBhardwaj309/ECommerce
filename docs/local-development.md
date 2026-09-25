# Local Development

## Requirements

- .NET 8 SDK
- Docker Engine with the Compose plugin

## Start infrastructure

In the repository root, create a local environment file from the template and set a strong local SQL Server password:

```powershell
Copy-Item .env.example .env
```

Edit `.env`, then start the infrastructure:

```powershell
docker compose up -d
docker compose ps
```

Endpoints: SQL Server `localhost,1433`, Redis `localhost:6379`, Kafka `localhost:29092`, and Kafka UI `http://localhost:8088`.

Stop the containers while retaining data with `docker compose down`. To delete local database, cache, and broker data, use `docker compose down -v`.

## Build and run API hosts

Build all projects:

```powershell
dotnet build ECommerce.sln
```

Run each command in a separate terminal. The ports match the gateway's local routing configuration.

```powershell
dotnet run --project src/Services/Identity/Identity.Api --urls http://localhost:5101
dotnet run --project src/Services/Product/Product.Api --urls http://localhost:5102
dotnet run --project src/Services/Cart/Cart.Api --urls http://localhost:5103
dotnet run --project src/Services/Order/Order.Api --urls http://localhost:5104
dotnet run --project src/Services/Inventory/Inventory.Api --urls http://localhost:5105
dotnet run --project src/Services/Payment/Payment.Api --urls http://localhost:5106
dotnet run --project src/Services/Notification/Notification.Api --urls http://localhost:5107
```

Start the gateway in another terminal:

```powershell
dotnet run --project src/Gateway/ApiGateway --urls http://localhost:5100
```

Probe a host directly at `/health`, `/live`, or `/ready`; the gateway's own probes are available on port `5100`. Gateway business routes are configured now, but service business endpoints arrive in later phases.