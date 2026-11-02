# My Books Kanban

Tablero Kanban para organizar tu biblioteca personal. Cada libro es un **lomo** de cuero que se mueve entre las columnas **Por leer**, **Leyendo** y **Leído**, con portada, categoría, progreso de lectura y valoración.

## Características

- Tablero Kanban con tres estados de lectura (`ToRead`, `Reading`, `Read`).
- Alta, edición, detalle y borrado de libros (título, autor, descripción, ISBN, páginas, página actual, valoración, fechas).
- Categorías gestionables desde la propia aplicación.
- Subida de portadas, con **color del lomo propuesto automáticamente** a partir del color dominante de la imagen (editable por el usuario).
- Búsqueda, filtros y paginación.
- Persistencia en SQLite; migraciones y datos iniciales se aplican al arrancar.
- Endpoint de salud en `/health`.

## Stack

- Blazor Web App (.NET 10), modo Interactive Server
- EF Core 9 + SQLite
- Tailwind CSS 3.4 (binario standalone) + CSS Isolation por componente
- SixLabors.ImageSharp 3.1.x (análisis de color de portadas)
- Tests: xUnit + bUnit

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Binario standalone de Tailwind CSS **v3.4.17** en `tools/tailwindcss` (no se versiona en git)
- (Opcional) Docker, para ejecutar la imagen
- (Opcional) `dotnet-ef` para crear migraciones

### Instalar el binario de Tailwind

```bash
mkdir -p tools
# Linux x64; usa tailwindcss-linux-arm64, tailwindcss-macos-arm64, etc. según tu plataforma
curl -fsSL -o tools/tailwindcss \
  https://github.com/tailwindlabs/tailwindcss/releases/download/v3.4.17/tailwindcss-linux-x64
chmod +x tools/tailwindcss
```

El build de .NET lo invoca automáticamente y regenera `src/wwwroot/app.css`. En configuración `Release` el build falla si el binario no existe.

## Puesta en marcha

```bash
git clone git@github.com:4l3j0Ok/my-books-kanban.git
cd my-books-kanban

dotnet run --project src/my-books-kanban.csproj
```

Al iniciar se aplican las migraciones y se cargan los datos iniciales. En desarrollo la base de datos es `my-books-kanban.dev.db`. Abre la URL que muestre la consola.

## Comandos útiles

```bash
dotnet build src/my-books-kanban.csproj   # compila .NET y Tailwind
dotnet test                               # ejecuta todos los tests

# Nueva migración (requiere: dotnet tool install --global dotnet-ef --version 9.0.4)
dotnet ef migrations add NombreMigracion \
  --project src/my-books-kanban.csproj \
  --output-dir Infrastructure/Migrations
```

## Configuración

| Variable | Descripción | Valor por defecto |
| --- | --- | --- |
| `ConnectionStrings__Default` | Cadena de conexión SQLite | `Data Source=my-books-kanban.db` |
| `ASPNETCORE_ENVIRONMENT` | Entorno; si no se define ninguna variable de entorno, `Program.cs` usa `Development` | `Development` (local) |
| `DataProtection__KeysPath` | Carpeta donde persistir las claves de Data Protection | — (en Docker: `/data/keys`) |

Las portadas subidas se guardan en `src/wwwroot/uploads/covers/` (ignorado por git).

## Estructura del proyecto

```
src/
├── Components/       # UI Blazor (Books, Categories, Kanban, Layout, Pages, Shared)
├── Application/      # Servicios, validadores, estado de UI, colores (SpinePalette, etc.)
├── Domain/           # Entidades y enums; no depende de nada
├── Infrastructure/   # DbContext, configuraciones EF, migraciones, seeding
└── wwwroot/          # Estáticos; app.src.css es la fuente de Tailwind
tests/MyBooksKanban.Tests/   # Tests de dominio, servicios y componentes
deploy/                      # Compose para despliegue
```

Dependencias entre capas: `Components → Application → Infrastructure → SQLite`. Los servicios reciben `IDbContextFactory<LibraryDbContext>` directamente (sin repositorio genérico).

## Tests

```bash
dotnet test
```

Cada test usa su propia base SQLite temporal, así que se pueden ejecutar en paralelo sin tocar tus datos.

## Docker

Construir y ejecutar la imagen localmente:

```bash
docker build -t my-books-kanban .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Default="Data Source=/data/my-books-kanban.db" \
  -v kb-data:/data -v kb-covers:/app/wwwroot/uploads \
  my-books-kanban
```

La aplicación queda en <http://localhost:8080>. Los volúmenes `/data` (base de datos y claves) y `/app/wwwroot/uploads` (portadas) deben persistirse.

Para desplegar la imagen publicada en GHCR hay un compose de ejemplo en `deploy/compose.yaml`:

```bash
docker compose -f deploy/compose.yaml up -d
```

## CI/CD

Los workflows de `.github/workflows/` se ejecutan al subir un tag de versión (`vX.Y.Z` o `X.Y.Z`) o manualmente:

- **ci.yml**: restaura, compila en Release, comprueba que `app.css` está actualizado y ejecuta los tests.
- **docker.yml**: construye la imagen, hace un smoke test contra `/health` y la publica en GHCR.

> Si cambias estilos, compila antes de hacer commit y sube el `src/wwwroot/app.css` regenerado: CI falla si está desactualizado.

## Convención de commits

Se usan [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.

## Notas para desarrollo

- **ImageSharp**: mantener la versión 3.x; la 4+ requiere licencia de pago.
- **Color del lomo**: `Book.SpineColor` pertenece al usuario; `null` significa "usar el color de la categoría". `SpinePalette` solo normaliza el color al renderizar, nunca modifica lo guardado.
- **Estilos**: Tailwind + CSS Isolation por componente. La textura de cuero (`.spine-face`) vive en `app.src.css` para que el formulario y el estante rendericen el mismo objeto.
- La advertencia NuGet `NU1903` (SQLitePCLRaw) está suprimida en `Directory.Build.props`.
