# TripGenius backend reference

## Solution layout

```
TripGeniusBackend.Domain/
TripGeniusBackend.Application/
  DTOs/          — request/response records per area
  Exceptions/    — AppException
  Interfaces/
    Repositories/
    Queries/
    UseCases/
    Services/    — cross-cutting app contracts (IJwtService, IEmbeddingService, …)
  Settings/      — *Settings bound via IOptions
  UseCases/      — *Service implementations
TripGeniusBackend.Infrastructure/
  Persistence/
    AppDbContext.cs
    Repositories/
    Queries/
    Services/    — JWT, email, AI, embeddings, file upload, …
    Hubs/
  Migrations/
TripGeniusBackend.API/
  Controllers/
  Middleware/
  Program.cs
TripGeniusBackend.Tests/
  Unit/Services/
  Integration/Controllers/
  Fixtures/      — TripGeniusWebApplicationFactory
  Builders/
```

## Key packages (Infrastructure)

- `Npgsql.EntityFrameworkCore.PostgreSQL` + `Pgvector` + `Pgvector.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore` 10.x
- JWT: `Microsoft.AspNetCore.Authentication.JwtBearer`

## Database (local / Docker)

- Image: `pgvector/pgvector:pg17`
- Connection key: `ConnectionStrings__DefaultConnection` or env `ConnectionStrings__DefaultConnection`
- `Program.cs` pattern:

```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, o => {
        o.UseVector();
        o.MigrationsHistoryTable("__EFMigrationsHistory", "public");
    }));
```

## Entities with embeddings

| Entity | Property | Column (PostgreSQL) |
|--------|----------|---------------------|
| `Trip` | `Vector? Embedding` | `vector(2048)` |
| `AiMemory` | `Vector Embedding` | `vector(2048)` |

## Exception mapping (API middleware)

| Exception | HTTP |
|-----------|------|
| `AppException` | `StatusCode` from exception |
| `ArgumentException` | 400 |
| `UnauthorizedAccessException` | 403 |
| `KeyNotFoundException` | 404 |
| `InvalidOperationException` | 409 |
| Other | 500 (logged) |

## EF migration commands

From repo root:

```bash
dotnet ef migrations add <MigrationName> --project TripGeniusBackend.Infrastructure --startup-project TripGeniusBackend.API
dotnet ef database update --project TripGeniusBackend.Infrastructure --startup-project TripGeniusBackend.API
```

## Target framework

`net10.0` — align new package references with existing 10.x Microsoft.* versions in csproj files.
