# syntax=docker/dockerfile:1

# ---------- Etapa SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia y restaura primero para aprovechar caché de capas.
COPY ["src/my-books-kanban.csproj", "./src/"]
COPY ["src/Directory.Build.props", "./src/"]
COPY ["src/Directory.Build.targets", "./src/"]
RUN dotnet restore "src/my-books-kanban.csproj"

# Copia el resto del código fuente.
COPY . .

# Binario standalone de Tailwind (no se versiona en git); obligatorio en Release.
ARG TARGETARCH
ARG TAILWIND_VERSION=3.4.17
RUN case "${TARGETARCH}" in \
      amd64) TW_ARCH=x64 ;; \
      arm64) TW_ARCH=arm64 ;; \
      *) echo "Arquitectura no soportada: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && mkdir -p tools \
    && curl -fsSL -o tools/tailwindcss \
       "https://github.com/tailwindlabs/tailwindcss/releases/download/v${TAILWIND_VERSION}/tailwindcss-linux-${TW_ARCH}" \
    && chmod +x tools/tailwindcss

# Compila y publica (el target CompileTailwind regenera app.css).
RUN dotnet publish "src/my-books-kanban.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Etapa runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /data /app/wwwroot/uploads/covers \
    && chown -R $APP_UID:$APP_UID /data /app/wwwroot/uploads

# Se ejecuta como usuario no root.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Volúmenes para datos persistentes.
VOLUME ["/data", "/app/wwwroot/uploads"]

# Healthcheck contra el endpoint /health.
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health >/dev/null 2>&1 || exit 1

ENTRYPOINT ["dotnet", "my-books-kanban.dll"]
