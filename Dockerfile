# ============================================================================
# Multi-stage Dockerfile para Microservicio Users
# ============================================================================
# STAGE 1: Build - Compila la aplicación
# STAGE 2: Runtime - Imagen optimizada para producción
# ============================================================================

# ============ STAGE 1: build ============
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar global.json para asegurar versión correcta del SDK
COPY global.json ./

# Copiar archivos de solución y gestión de paquetes
COPY Users.sln ./
COPY Directory.Packages.props ./

# Copiar archivos .csproj (optimiza cache de Docker - solo se invalida si cambian las dependencias)
COPY src/Users.Api/Users.Api.csproj                      src/Users.Api/
COPY src/Users.Application/Users.Application.csproj      src/Users.Application/
COPY src/Users.Domain/Users.Domain.csproj                src/Users.Domain/
COPY src/Users.Infrastructure/Users.Infrastructure.csproj src/Users.Infrastructure/
COPY src/Users.Tests/Users.Tests.csproj                  src/Users.Tests/

# Restaurar dependencias (capa independiente para aprovechar cache)
RUN dotnet restore Users.sln

# Copiar el resto del código fuente
COPY src/ src/

# Publicar aplicación (--no-restore evita restore duplicado)
RUN dotnet publish ./src/Users.Api/Users.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============ STAGE 2: runtime ============
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Instalar curl para health checks y crear usuario no-root para seguridad (necesario para Docker HEALTHCHECK)
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    groupadd -r appuser && useradd -r -g appuser appuser

# Copiar binarios publicados desde stage de build
COPY --from=build /app/publish .

# Cambiar permisos y usuario
RUN chown -R appuser:appuser /app
USER appuser

# Exponer puerto
EXPOSE 8081

# Health check integrado
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8081/health || exit 1

# Punto de entrada
ENTRYPOINT ["dotnet", "Users.Api.dll", "--urls", "http://0.0.0.0:8081"]