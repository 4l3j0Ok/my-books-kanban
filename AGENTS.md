# My Books Kanban — Agent Instructions

## Stack
- Blazor Web App (.NET 10) Interactive Server
- EF Core 9 + SQLite via `IDbContextFactory<LibraryDbContext>`
- Tailwind CSS via standalone CLI (`tools/tailwindcss`)
- xUnit + bUnit + SQLite temp DB per test

## Commands
```bash
dotnet build src/my-books-kanban.csproj           # also compiles Tailwind via MSBuild target
dotnet run --project src/my-books-kanban.csproj    # auto-applies migrations + seeds data
dotnet test                                        # runs all tests
dotnet ef migrations add Name --project src/my-books-kanban.csproj --output-dir Infrastructure/Migrations
docker compose up --build                          # app on :8080
```

## Architecture
- Single project (not a multi-project solution), modular by folders: `Components → Application → Infrastructure → SQLite`, `Domain` depends on nothing
- No generic repository — `IDbContextFactory<LibraryDbContext>` injected directly into services
- `KanbanState` (Scoped) holds UI state; DB is source of truth
- Form models (`BookFormModel`) separate from domain entities to avoid binding entity graph
- Migrations + seeding run at startup via `DbSeeder.SeedAsync` (which calls `db.Database.MigrateAsync`)
- Blazor rendering mode: Interactive Server throughout

## Testing
- `TestDbContextFactory.Create()` returns a factory pointing to a unique temp SQLite file per test
- Test classes implement `IAsyncLifetime`; `InitializeAsync` seeds the db via `DbSeeder.SeedAsync`
- Key test files: `tests/MyBooksKanban.Tests/Application/BookServiceTests.cs`, `Application/CategoryServiceTests.cs`, `Domain/BookValidationTests.cs`, `Components/BookFormTests.cs`

## Toolchain quirks
- **Tailwind binary** `tools/tailwindcss` must exist for local builds — auto-invoked by `Directory.Build.targets` (`CompileTailwind` target)
- NuGet warning `NU1903` (SQLitePCLRaw vulnerability) suppressed in `Directory.Build.props`
- Program.cs sets `ASPNETCORE_ENVIRONMENT=Development` if both `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` are empty; Docker compose passes Production explicitly
- Dev DB: `my-books-kanban.dev.db`; Prod DB: `my-books-kanban.db` — both gitignored
- Connection string override: `ConnectionStrings__Default` env var
- Cover uploads: `src/wwwroot/uploads/covers/` (gitignored except `.gitkeep`)
- Latest EF tool for migrations: `dotnet tool install --global dotnet-ef --version 9.0.4`
- No Bootstrap — Tailwind + CSS Isolation per component

## CI/CD (`.github/workflows/`)
- **ci.yml**: push/PR to main/develop → restore → build Release → test → publish artifact
- **docker.yml**: push to main → Docker build → smoke test (`/health` endpoint) → push to GHCR
- **deploy.yml**: manual `workflow_dispatch` → build → push → SSH deploy

## Skills available
- `frontend-design` registered in `skills-lock.json` — load via `skill("frontend-design")` when making UI changes

## Commit convention
[Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.
