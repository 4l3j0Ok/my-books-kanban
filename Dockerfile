# syntax=docker/dockerfile:1

# ---------- Etapa SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia y restaura primero para aprovechar caché de capas.
COPY ["src/my-books-kanban.csproj", "./src/"]
COPY ["src/Directory.Build.props", "./src/"]
COPY ["src/Directory.Build.targets", "./src/"]
RUN dotnet restore "src/my-books-kanban.csproj"

# Copia el resto y compila (el target CompileTailwind se ejecuta automáticamente).
COPY . .
RUN dotnet publish "src/my-books-kanban.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Etapa runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Se ejecuta como usuario no root cuando es posible.
USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Volúmenes para datos persistentes.
VOLUME ["/data", "/app/wwwroot/uploads"]

# Healthcheck contra el endpoint /health.
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health >/dev/null 2>&1 || exit 1

ENTRYPOINT ["dotnet", "my-books-kanban.dll"]
