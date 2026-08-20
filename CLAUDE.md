# pharmacy-api (PharmaCare PMS)

.NET 10 / ASP.NET Core pharmacy management system backend, PostgreSQL via
EF Core. The `pharmacy-ui` directory is a separate Git repository (submodule)
— a Next.js 16 frontend.

## Solution layout

- `PMS/` — the Web API project (entry point, `PMS.csproj`, controllers, DI wiring)
- `PMS.Application/` — application/service layer
- `PMS.Repository/` — EF Core `DbContext`, migrations, repositories
- `PMS.Models/` — entities and shared model types
- `PMS.Common/` — cross-cutting helpers used by the above
- `PMS.Tests/` — xUnit test project (EF Core InMemory, Moq, `Microsoft.AspNetCore.Mvc.Testing`) — no external database needed to run tests

## Local development

```bash
dotnet restore PMS.sln
dotnet build PMS.sln
dotnet test PMS.Tests/PMS.Tests.csproj

# Run the API against Docker (reads .env — copy from .env.example first):
docker compose up -d
```

## CI/CD

Gitea Actions workflows live in `.gitea/workflows/`. See `docs/ci-cd.md` for
the full pipeline, required secrets/variables, and the release process.
