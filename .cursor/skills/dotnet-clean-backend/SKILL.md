---
name: dotnet-clean-backend
description: >-
  Builds and maintains .NET backends with Clean Architecture, PostgreSQL, EF Core,
  and pgvector. Use when adding features, APIs, entities, migrations, repositories,
  use cases, vector search, or DI in ASP.NET Core projects with layered Domain /
  Application / Infrastructure / API structure.
---

# .NET Clean Backend (PostgreSQL + pgvector)

## Layer rules

Respect dependency direction. **Never** reference Infrastructure or API from Domain or Application.

| Layer | Project suffix | Contains | Must not reference |
|-------|--------------|----------|-------------------|
| Domain | `.Domain` | Entities, enums, domain methods | EF, HTTP, DTOs, services |
| Application | `.Application` | Use cases, DTOs, interfaces, `AppException`, settings | EF, Npgsql, controllers |
| Infrastructure | `.Infrastructure` | `DbContext`, repositories, query services, external APIs | Controllers |
| API | `.API` | Controllers, hubs, middleware, `Program.cs`, health checks | Direct `DbContext` in controllers |

## Adding a feature (checklist)

Copy and track:

```
- [ ] Domain: entity / enum / behavior
- [ ] Application: DTOs, IRepository / IQueryService / IUseCase interfaces
- [ ] Application: use case (*Service in UseCases/)
- [ ] Infrastructure: repository + query service implementations
- [ ] Infrastructure: EF configuration in AppDbContext.OnModelCreating
- [ ] Infrastructure: EF migration (if schema changed)
- [ ] API: thin controller endpoint
- [ ] Program.cs: register interfaces (Scoped for DB-bound types)
- [ ] Tests: unit (use case) and/or integration (WebApplicationFactory)
```

## Use cases vs persistence

- **Use cases** (`*Service` implementing `I*Service`): orchestration, authorization checks, mapping to DTOs, calling repositories/query services. Throw `AppException(statusCode, message)` for expected failures.
- **Repositories** (`I*Repository`): commands — add, update, delete, `SaveChanges`, targeted loads with `Include`.
- **Query services** (`I*QueryService`): read-only projections and list/detail queries that do not belong on write repositories.

Controllers stay thin: validate binding, call one use-case method, return `Ok` / `Created` / `NoContent`. Prefer letting `ExceptionMiddleware` handle `AppException` instead of duplicating try/catch per action.

## PostgreSQL + EF Core

- Use **Npgsql** with a shared `NpgsqlDataSource` when using pgvector: `UseVector()` on the data source builder and `UseNpgsql(dataSource, o => o.UseVector())` on `DbContext`.
- Enable extension in model: `modelBuilder.HasPostgresExtension("vector")` and `HasDefaultSchema("public")`.
- Migrations: add from Infrastructure project (`dotnet ef migrations add <Name> --project TripGeniusBackend.Infrastructure --startup-project TripGeniusBackend.API`). Apply at startup only when `Database.IsRelational()`.
- For **InMemory** tests: convert `Vector` properties with string conversions; do not rely on pgvector operators in memory.

## pgvector

- Domain entities store embeddings as `Pgvector.Vector` (nullable when optional).
- Column type must match model dimension, e.g. `vector(2048)` — keep entity, `OnModelCreating`, and migration in sync.
- Similarity search in repositories:

```csharp
var vector = new Vector(queryEmbedding);
await _context.Entities
    .Where(e => e.Embedding != null)
    .OrderBy(e => e.Embedding!.CosineDistance(vector))
    .Take(limit)
    .ToListAsync();
```

- Generate embeddings in Infrastructure (`IEmbeddingService` + `HttpClient`); pass `float[]` into repositories. Do not call embedding HTTP APIs from Domain.
- After altering vector dimensions, add an explicit migration (`AlterColumn` / raw SQL) — EF will not always infer dimension changes safely.

## DI conventions

| Lifetime | Use for |
|----------|---------|
| Scoped | DbContext, repositories, query services, use cases, per-request services |
| Singleton | Stateless infrastructure shared across requests (e.g. notification broadcaster) |
| Transient | Lightweight, stateless third-party clients when required |

Register interfaces in `Program.cs` grouped by Application vs Infrastructure. Use `IOptions<TSettings>` for config sections under `Application/Settings/`.

## API and cross-cutting

- Routes: `[Route("api/[controller]")]`, JWT `[Authorize]` where needed.
- Global concerns in middleware order: forwarded headers → CORS → security headers → `ExceptionMiddleware` → auth → rate limiting on controllers.
- Secrets and connection strings from environment / `.env` (DotNetEnv), not hardcoded.
- Health: relational DB `CanConnectAsync` for readiness; external API keys for liveness where applicable.

## Testing

- **Unit**: mock `I*Repository` / `I*QueryService`; test use case logic and exceptions.
- **Integration**: `WebApplicationFactory` with InMemory or test PostgreSQL; replace email/external services with no-op fakes.
- Builders in `Tests/Builders/` for entity setup.

## Quality bar

- Nullable reference types enabled; avoid null-forgiving unless justified.
- Async all the way: `async Task` / `Task<T>`, no `.Result` / `.Wait()`.
- No business logic in controllers or `Program.cs` beyond composition.
- Match existing naming: `TripRequest`, `TripResponse`, `ITripService`, `TripRepository`, `TripQueryService`.

## Project-specific reference

For TripGenius layer layout, packages, and docker DB image, see [reference.md](reference.md).
