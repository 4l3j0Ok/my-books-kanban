# My Books Kanban

Un tablero Kanban personal para llevar el seguimiento de libros, construido con **Blazor Web App** (Interactive Server) sobre **.NET 10**, con persistencia en **SQLite** mediante **EF Core 9** y estilos con **Tailwind CSS**.

Los libros se mueven entre columnas (p. ej. *Pendiente → Leyendo → Terminado*), se agrupan por categoría y soportan subida de portadas.

## Stack

- **Blazor Web App** — renderizado Interactive Server en toda la app
- **.NET 10** (`net10.0`)
- **EF Core 9.0.4** + **SQLite** (vía `IDbContextFactory<LibraryDbContext>`)
- **Tailwind CSS** (CLI independiente, sin Node/npm)
- **xUnit** + **bUnit** para tests (archivo SQLite temporal por test)

## Inicio rápido

```bash
# Compilar (también ejecuta el target MSBuild de Tailwind)
dotnet build src/my-books-kanban.csproj

# Ejecutar — aplica migraciones y siembra datos al arrancar
dotnet run --project src/my-books-kanban.csproj

# Correr toda la suite de tests
dotnet test

# Agregar una nueva migración de EF
dotnet tool install --global dotnet-ef --version 9.0.4
dotnet ef migrations add Nombre --project src/my-books-kanban.csproj --output-dir Infrastructure/Migrations

# Levantar con Docker (app en http://localhost:8080)
docker compose up --build
```

## Estructura del proyecto

Es una solución de **un solo proyecto** (no hay layout multi-proyecto). El código se organiza por carpetas en capas claras, con `Domain` en el núcleo y sin dependencias hacia arriba.

```
my-books-kanban/
├── src/                            # Código fuente de la aplicación
│   ├── my-books-kanban.csproj      # Proyecto Web SDK (.NET 10)
│   ├── Program.cs                  # Bootstrap del host, DI, migraciones + seed al arrancar
│   ├── appsettings*.json           # Config por entorno
│   │
│   ├── Directory.Build.props       # Propiedades MSBuild compartidas
│   ├── Directory.Build.targets     # Target MSBuild `CompileTailwind`
│   │
│   ├── Domain/                     # Modelo de dominio puro — sin dependencias
│   │   ├── Entities/               #   Book, Category, …
│   │   └── Enums/                  #   BookStatus, etc.
│   │
│   ├── Application/                # Casos de uso, estado, contratos
│   │   ├── Interfaces/             #   Contratos de servicios (IBookService, ICategoryService, …)
│   │   ├── Services/               #   Implementaciones (hablan con EF Core)
│   │   ├── Models/                 #   DTOs / form models (p. ej. BookFormModel)
│   │   ├── Validators/             #   Reglas de validación de dominio
│   │   └── State/                  #   KanbanState — estado de UI Scoped (la DB es la fuente de verdad)
│   │
│   ├── Infrastructure/             # EF Core + almacenamiento
│   │   ├── Data/                   #   LibraryDbContext, DbSeeder
│   │   ├── Configurations/         #   Mapeos IEntityTypeConfiguration<…>
│   │   ├── Migrations/             #   Migraciones generadas por EF Core
│   │   └── Storage/                #   Manejador de subida de portadas
│   │
│   ├── Components/                 # UI de Blazor (Razor)
│   │   ├── App.razor               #   Componente raíz
│   │   ├── Routes.razor            #   Router
│   │   ├── _Imports.razor          #   Usings compartidos
│   │   ├── Layout/                 #   MainLayout, NavMenu, …
│   │   ├── Pages/                  #   Páginas enrutables
│   │   ├── Kanban/                 #   Tablero + componentes de columna
│   │   ├── Books/                  #   Card de libro, formulario, detalles
│   │   └── Shared/                 #   UI reutilizable (modales, inputs, …)
│   │
│   └── wwwroot/                    # Assets estáticos
│       ├── app.css / app.src.css   # Entrada de Tailwind + salida compilada
│       ├── favicon.png
│       ├── images/                 # Imágenes estáticas
│       ├── js/                     # Scripts de cliente
│       └── uploads/                # Portadas subidas por el usuario (gitignored)
│
├── tests/                          # Proyecto de tests
│   └── MyBooksKanban.Tests/        # xUnit + bUnit
│       ├── MyBooksKanban.Tests.csproj
│       ├── TestDbContextFactory.cs # Factory de SQLite temporal por test
│       ├── Application/            # BookServiceTests, CategoryServiceTests, …
│       ├── Domain/                 # BookValidationTests, …
│       └── Components/             # BookFormTests (bUnit), …
│
├── tools/
│   └── tailwindcss                 # Binario del CLI independiente de Tailwind (requerido para builds locales)
│
├── .github/
│   ├── workflows/                  # Pipelines de CI/CD (ci.yml, docker.yml, deploy.yml)
│   ├── ISSUE_TEMPLATE/
│   └── PULL_REQUEST_TEMPLATE.md
│
├── .agents/                        # Skills del agente (p. ej. frontend-design)
├── .agents/skills/
│
├── Dockerfile                      # Build multi-etapa, corre en :8080, non-root, healthcheck en /health
├── compose.yaml                    # Orquestación local con Docker
├── tailwind.config.js              # Globs de contenido de Tailwind
├── skills-lock.json                # Skills del agente fijados por versión
├── AGENTS.md                       # Reglas del proyecto orientadas al agente
└── README.md
```

## Notas de arquitectura

- **Capas por carpeta, no por proyecto.** La dirección de dependencias es estricta:
  `Components → Application → Infrastructure → SQLite`, y `Domain` no depende de nada.
- **Sin repositorio genérico.** Los servicios reciben `IDbContextFactory<LibraryDbContext>` directamente y construyen las consultas que necesitan.
- **`KanbanState` es `Scoped`.** Guarda estado de UI transitorio (libro seleccionado, estado de drag, filtros), pero la **base de datos es la fuente de verdad** del tablero.
- **Los form models están desacoplados de las entidades.** Los componentes se enlazan a `BookFormModel` (y similares) en lugar del grafo de la entidad, para mantener la validación y el binding predecibles.
- **Migraciones + seeding al arrancar.** `DbSeeder.SeedAsync` invoca `db.Database.MigrateAsync()`, así que un deploy nuevo se auto-inicializa.
- **Renderizado Interactive Server en toda la app** — sin WebAssembly, sin auto-cambio entre modos.

## Configuración

| Variable / ajuste | Por defecto | Notas |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` (si ASPNETCORE/DOTNET están vacías) | `compose.yaml` y `Dockerfile` fuerzan `Production` |
| `ConnectionStrings__Default` | `my-books-kanban.dev.db` (dev) / `my-books-kanban.db` (prod) | Ambos archivos están en `.gitignore`. En compose se usa `/data/my-books-kanban.db` |
| Subida de portadas | `src/wwwroot/uploads/covers/` | En `.gitignore` salvo `.gitkeep` |
| Puerto HTTP | `5000` (dev) / `8080` (Docker) | |

## Particularidades del toolchain

- **`tools/tailwindcss`** debe existir para los builds locales. El target MSBuild `CompileTailwind` (en `src/Directory.Build.targets`) lo invoca automáticamente al compilar/publicar.
- **Warning de NuGet `NU1903`** (vulnerabilidad de SQLitePCLRaw) está suprimido en `src/Directory.Build.props`.
- **Sin Bootstrap.** Los estilos son **Tailwind** + **CSS Isolation por componente** (archivos `.razor.css`).
- **Versión de la herramienta de EF:** instalar con `dotnet tool install --global dotnet-ef --version 9.0.4`.

## Testing

- `TestDbContextFactory.Create()` devuelve una factory respaldada por un **archivo SQLite temporal único por test**, así que los tests están totalmente aislados.
- Las clases de test implementan `IAsyncLifetime`; `InitializeAsync` llama a `DbSeeder.SeedAsync` para dejar una base conocida.
- Archivos de test clave:
  - `tests/MyBooksKanban.Tests/Application/BookServiceTests.cs`
  - `tests/MyBooksKanban.Tests/Application/CategoryServiceTests.cs`
  - `tests/MyBooksKanban.Tests/Domain/BookValidationTests.cs`
  - `tests/MyBooksKanban.Tests/Components/BookFormTests.cs` (bUnit)

## CI/CD

Definido en `.github/workflows/`:

- **`ci.yml`** — push/PR a `main` o `develop` → restore → build `Release` → test → publicar artefacto.
- **`docker.yml`** — push a `main` → build Docker → smoke test contra `/health` → push de la imagen a GHCR.
- **`deploy.yml`** — `workflow_dispatch` manual → build → push → deploy por SSH.

## Convención de commits

[Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.
