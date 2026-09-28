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

Compose initializes `ProductDb`, `OrderDb`, `InventoryDb`, `PaymentDb`, and `IdentityDb` on SQL Server. The initialization is safe to rerun. Each service's `appsettings.Development.json` contains its database connection string with a blank password; set the password to the same value as `MSSQL_SA_PASSWORD` in your local `.env` before connecting. Do not commit local credentials.

Endpoints: SQL Server `localhost,1433`, Redis `localhost:6379`, Kafka `localhost:29092`, and Kafka UI `http://localhost:8088`.

Stop the containers while retaining data with `docker compose down`. To delete local database, cache, and broker data, use `docker compose down -v`.

## Build and run API hosts

### Identity authentication

The Identity API stores users and roles in `IdentityDb` using ASP.NET Core Identity's password hasher. Before starting it, configure a JWT signing key and an initial administrator with .NET user-secrets. Use a random signing key of at least 32 bytes and a bootstrap password meeting the policy (12+ characters, uppercase, lowercase, digit, special character, and at least four unique characters):

```powershell
dotnet user-secrets set "Jwt:SigningKey" "<random-signing-key-at-least-32-bytes>" --project src/Services/Identity/Identity.Api
dotnet user-secrets set "Identity:BootstrapAdminEmail" "admin@example.com" --project src/Services/Identity/Identity.Api
dotnet user-secrets set "Identity:BootstrapAdminPassword" "<strong-bootstrap-password>" --project src/Services/Identity/Identity.Api
```

If the Identity connection string is not already in the ignored local Development settings, configure `ConnectionStrings:IdentityDbSqlConnection` with user-secrets as well. Apply the schema before starting the API:

```powershell
dotnet ef database update --project src/Services/Identity/Identity.Infrastructure --startup-project src/Services/Identity/Identity.Api
```

The initial Admin is optional, but without one only `Customer` accounts can be created and the Admin-only role-assignment endpoint cannot be used. Public registration always assigns `Customer`; clients cannot select their own role.

Identity endpoints:

- `POST /api/identity/auth/register`
- `POST /api/identity/auth/login`
- `POST /api/identity/auth/refresh`
- `POST /api/identity/auth/revoke`
- `GET /api/identity/auth/me` (authenticated)
- `GET /api/identity/users/{userId}` (self or Admin)
- `POST /api/identity/admin/users/{userId}/roles` (Admin)

Access tokens are short-lived JWTs. Refresh tokens are single-use, rotated on refresh, and stored in the database only as SHA-256 hashes. Login failures are generic and trigger account lockout after repeated failures. Try the request examples in `src/Services/Identity/Identity.Api/Identity.http`.

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