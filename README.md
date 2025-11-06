# 👥 Accessibility Users Service

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-386%2F386-brightgreen)](test-dashboard.html)
[![Coverage](https://img.shields.io/badge/coverage-94.71%25-brightgreen)](coverage-report/index.html)
[![License](https://img.shields.io/badge/license-Proprietary-red)](LICENSE)

> **Microservicio de gestión de usuarios y autenticación desarrollado en .NET 9 con Clean Architecture. Proporciona gestión completa de usuarios, preferencias de accesibilidad, sesiones y autenticación JWT.**

> ⚡ **Nota:** Este microservicio forma parte de un ecosistema donde el **Gateway** gestiona rate limiting, caching (Redis), circuit breaker y load balancing. El microservicio se enfoca en su lógica de dominio específica.

## 📋 Descripción

Microservicio empresarial para:

- **Gestión de usuarios** con operaciones CRUD completas
- **Autenticación JWT** con login, logout y recuperación de contraseña
- **Preferencias de accesibilidad** personalizadas por usuario
- **Gestión de sesiones** con control de sesiones activas
- **i18n integrado** con soporte multiidioma (es, en, pt)

## ✨ Características

### 👤 Gestión de Usuarios

- **CRUD completo** de usuarios con validación
- Búsqueda por email con unicidad garantizada
- Eliminación de usuarios y datos asociados
- Creación de usuarios con preferencias incluidas
- Actualización masiva de usuarios con preferencias

### 🔐 Autenticación & Seguridad

- **JWT Authentication** con tokens seguros
- Login con email/contraseña
- Logout con invalidación de tokens
- Reset de contraseña con confirmación por email
- Confirmación de email para activación de cuentas

### ⚙️ Preferencias de Accesibilidad

- **Configuración personalizada** por usuario
- Preferencias de contraste, tamaño de fuente, modo oscuro
- Lector de pantalla, navegación por teclado
- Animaciones reducidas y otras opciones WCAG
- CRUD completo de preferencias

### 📱 Gestión de Sesiones

- **Control de sesiones activas** por usuario
- Listado de todas las sesiones
- Cierre de sesión específica por ID
- Cierre masivo de sesiones por usuario
- Auditoría de sesiones activas

### 🌐 i18n & Accesibilidad

- Soporte multiidioma (es, en, pt)
- Mensajes de error localizados
- Content negotiation automático
- Headers de idioma en responses

### 🏥 Health Checks

- Database connectivity check
- Application health monitoring
- Memory usage tracking
- Endpoints de salud personalizados

### 📊 Observabilidad & Métricas

- **Prometheus Metrics** integrado
- Métricas de negocio personalizadas (usuarios, logins, sesiones)
- Endpoint `/metrics` expuesto
- Monitoreo de autenticación y operaciones
- Histogramas de duración de operaciones
- Gauges de sesiones activas y usuarios totales

### ⏰ Gestión de Zona Horaria

- **DateTimeProvider Service** para manejo consistente de fechas
- Configuración de zona horaria Ecuador (America/Guayaquil, UTC-5)
- MySQL configurado con timezone local
- Entity Framework con ValueConverter para DateTime
- Todas las fechas se almacenan y muestran en hora de Ecuador

## 🏗️ Arquitectura

```
┌───────────────────────────────────────────────────┐
│          👥 USERS MICROSERVICE API                │
│                (Port 8081)                        │
│                                                   │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────┐ │
│  │ Controllers │  │  Middleware │  │  Health  │ │
│  │  (6 APIs)   │  │  (Context)  │  │  Checks  │ │
│  └─────────────┘  └─────────────┘  └──────────┘ │
│         │                │               │       │
│         └────────────────┴───────────────┘       │
│                      │                           │
│              ┌───────▼───────┐                   │
│              │  APPLICATION  │                   │
│              │   Services    │                   │
│              │   Use Cases   │                   │
│              └───────┬───────┘                   │
│                      │                           │
│              ┌───────▼───────┐                   │
│              │    DOMAIN     │                   │
│              │   Entities    │                   │
│              │  Interfaces   │                   │
│              └───────┬───────┘                   │
│                      │                           │
│              ┌───────▼───────┐                   │
│              │INFRASTRUCTURE │                   │
│              │   EF Core     │                   │
│              │   Repositories│                   │
│              └───────┬───────┘                   │
└──────────────────────┼───────────────────────────┘
                       │
                       ▼
               ┌──────────────┐
               │  MySQL DB    │
               │  (users_db)  │
               └──────────────┘
```

**Clean Architecture con 4 capas:**

- **API:** Controllers, Middleware, Health Checks
- **Application:** Services, DTOs, Use Cases
- **Domain:** Entities, Interfaces, Business Logic
- **Infrastructure:** EF Core, Repositories, MySQL

## 🚀 Quick Start

### Requisitos

- .NET 9.0 SDK
- MySQL 8.0+
- Docker & Docker Compose (opcional)

### Instalación Local

```bash
# Clonar repositorio
git clone https://github.com/your-org/accessibility-ms-users.git
cd accessibility-ms-users

# Configurar base de datos
mysql -u root -p < init-users-db.sql

# Configurar variables de entorno
cp .env.example .env
# Editar .env con tus credenciales de MySQL

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build --configuration Release

# Ejecutar
dotnet run --project src/Users.Api/Users.Api.csproj
```

### Uso con Docker Compose

```bash
# Levantar todos los servicios
docker-compose up -d

# Ver logs
docker-compose logs -f users-api

# Verificar estado
docker-compose ps

# Detener servicios
docker-compose down
```

### Verificación

```bash
# Health check
curl http://localhost:8081/health

# Crear usuario de prueba
curl -X POST http://localhost:8081/api/users \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'
```

## 📡 API Endpoints

### 🔐 Autenticación (/api/Auth)

| Método | Endpoint                   | Descripción                 |
| ------ | -------------------------- | --------------------------- |
| POST   | `/api/Auth/login`          | Login con email/password    |
| POST   | `/api/Auth/logout`         | Logout y cierre de sesión   |
| POST   | `/api/Auth/reset-password` | Reset de contraseña         |
| POST   | `/api/Auth/confirm-email`  | Confirmar email del usuario |

### 👤 Usuarios (/api/users)

| Método | Endpoint              | Descripción                          |
| ------ | --------------------- | ------------------------------------ |
| GET    | `/api/users`          | Listar todos los usuarios            |
| POST   | `/api/users`          | Crear nuevo usuario                  |
| GET    | `/api/users/by-email` | Buscar usuario por email             |
| DELETE | `/api/users`          | Eliminar usuario por ID              |
| DELETE | `/api/users/by-email` | Eliminar usuario por email           |
| DELETE | `/api/users/all-data` | Eliminar todos los datos del usuario |

### 👥 Usuarios con Preferencias (/api/users-with-preferences)

| Método | Endpoint                               | Descripción                       |
| ------ | -------------------------------------- | --------------------------------- |
| POST   | `/api/users-with-preferences`          | Crear usuario con preferencias    |
| PATCH  | `/api/users-with-preferences/by-email` | Actualizar usuario y preferencias |

### ⚙️ Preferencias (/api/preferences)

| Método | Endpoint                   | Descripción                         |
| ------ | -------------------------- | ----------------------------------- |
| GET    | `/api/preferences/by-user` | Obtener preferencias por usuario ID |
| POST   | `/api/preferences`         | Crear preferencias                  |
| DELETE | `/api/preferences`         | Eliminar preferencias               |

### 📱 Sesiones (/api/sessions)

| Método | Endpoint                | Descripción                             |
| ------ | ----------------------- | --------------------------------------- |
| GET    | `/api/sessions`         | Listar todas las sesiones activas       |
| GET    | `/api/sessions/user`    | Obtener sesiones por usuario            |
| DELETE | `/api/sessions`         | Cerrar sesión específica por ID         |
| DELETE | `/api/sessions/by-user` | Cerrar todas las sesiones de un usuario |

### 🏥 Health (/health)

| Método | Endpoint        | Descripción          |
| ------ | --------------- | -------------------- |
| GET    | `/health`       | Health check general |
| GET    | `/health/ready` | Readiness probe      |
| GET    | `/health/live`  | Liveness probe       |

### 📊 Metrics (/metrics)

| Método | Endpoint   | Descripción         |
| ------ | ---------- | ------------------- |
| GET    | `/metrics` | Métricas Prometheus |

**Total: 26 endpoints disponibles**

## 🧪 Testing

### Estado de Cobertura

**Estado General:** ✅ 386/386 tests exitosos (100%)  
**Cobertura Total:** 94.71% (1290/1362 líneas cubiertas)

| Capa                           | Cobertura | Tests                    | Estado |
| ------------------------------ | --------- | ------------------------ | ------ |
| **Users.Api**                  | 88.2%     | Controllers + Middleware | ✅     |
| AuthController                 | 100%      | Login, Logout, Reset     | ✅     |
| PreferenceController           | 99.1%     | CRUD Preferencias        | ✅     |
| SessionController              | 94.5%     | Gestión Sesiones         | ✅     |
| UserController                 | 93.1%     | CRUD Usuarios            | ✅     |
| UsersWithPreferencesController | 100%      | Usuarios + Prefs         | ✅     |
| **Users.Application**          | 95%+      | Services + DTOs          | ✅     |
| **Users.Domain**               | 100%      | Entities + Interfaces    | ✅     |
| **Users.Infrastructure**       | 85%+      | Repositories + EF        | ✅     |

**Métricas detalladas:**

- **Cobertura de líneas:** 94.71% (1290/1362)
- **Cobertura de ramas:** 90.93%
- **Tiempo de ejecución:** 17.6s para 386 tests
- **Tasa de éxito:** 100%

### Comandos de Testing

```bash
# Todos los tests con cobertura
.\manage-tests.ps1 -GenerateCoverage -OpenReport

# Solo tests unitarios
.\manage-tests.ps1 -TestType Unit

# Tests de integración
.\manage-tests.ps1 -TestType Integration

# Ver dashboard interactivo
Start-Process .\test-dashboard.html
```

### Categorías de Tests

**Unit Tests:**

- Validación de entidades (User, Preference, Session)
- Lógica de servicios (AuthService, UserService)
- DTOs y mappers
- Validadores de dominio

**Integration Tests:**

- Controllers con base de datos en memoria
- Repositorios con MySQL real
- Health checks completos
- Middleware de contexto de usuario

**E2E Tests:**

- Flows completos de autenticación
- Creación de usuario + preferencias
- Gestión de sesiones activas
- Recuperación de contraseña

## � Observabilidad & Métricas

### Prometheus Metrics

El microservicio expone métricas detalladas en el endpoint `/metrics` para monitoreo con Prometheus/Grafana.

#### 📈 Métricas de Negocio

**Contadores (Counters):**

```
# Total de usuarios registrados
users_registered_total

# Total de logins exitosos/fallidos
auth_login_total{status="success|failure"}

# Total de sesiones creadas
sessions_created_total

# Total de preferencias actualizadas
preferences_updated_total

# Total de password resets solicitados
password_resets_requested_total
```

**Histogramas (Histograms):**

```
# Duración de operaciones de autenticación
auth_operation_duration_seconds{operation="login|logout|reset"}

# Duración de consultas de usuarios
user_query_duration_seconds{operation="get_all|get_by_email|create"}

# Duración de operaciones de sesión
session_operation_duration_seconds{operation="create|close|get_active"}
```

**Gauges:**

```
# Sesiones activas actualmente
active_sessions_count

# Usuarios registrados totales
total_users_count
```

#### 🔍 Consultar Métricas

```bash
# Ver todas las métricas
curl http://localhost:8081/metrics

# Filtrar métricas de autenticación
curl http://localhost:8081/metrics | grep "auth_login_total"

# Verificar sesiones activas
curl http://localhost:8081/metrics | grep "active_sessions_count"
```

#### 📊 Dashboard Grafana (Ejemplo)

```yaml
# Panel 1: Tasa de registro de usuarios
rate(users_registered_total[5m])

# Panel 2: Tasa de login exitoso vs fallido
sum(rate(auth_login_total[5m])) by (status)

# Panel 3: Duración promedio de login
histogram_quantile(0.95, rate(auth_operation_duration_seconds_bucket{operation="login"}[5m]))

# Panel 4: Sesiones activas en tiempo real
active_sessions_count
```

### Health Checks

```bash
# Health check básico
curl http://localhost:8081/health

# Readiness (listo para recibir tráfico)
curl http://localhost:8081/health/ready

# Liveness (proceso está vivo)
curl http://localhost:8081/health/live
```

**Respuesta Health Check:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "Database connection is healthy",
      "duration": "00:00:00.0123456"
    },
    "memory": {
      "status": "Healthy",
      "description": "Memory usage is within limits",
      "data": {
        "allocatedMB": 128,
        "thresholdMB": 512
      },
      "duration": "00:00:00.0001234"
    },
    "application": {
      "status": "Healthy",
      "description": "Users API is running",
      "duration": "00:00:00.0001000"
    }
  }
}
```

## 🔐 Arquitectura de Seguridad

### Flujo de Autenticación

```
┌──────────┐    JWT    ┌─────────┐   X-User-*   ┌──────────┐   IUserContext   ┌────────────┐
│  Client  │ ───────>  │ Gateway │ ──────────>  │ Middleware│ ──────────────> │ Controller │
└──────────┘           └─────────┘              └──────────┘                 └────────────┘
                           │                          │                            │
                       Valida JWT              Extrae headers              Valida IsAuthenticated
                       + Secret               + Crea UserContext           + Usa UserId/Role
```

### Middleware Stack

**1. GatewaySecretValidationMiddleware**

```csharp
// Valida que la petición provenga del Gateway autorizado
// Header requerido: X-Gateway-Secret
// Configuración: appsettings.json -> Gateway:Secret
```

**2. UserContextMiddleware**

```csharp
// Extrae información de usuario de headers propagados por Gateway
// Headers procesados:
//   - X-User-Id          → UserId
//   - X-User-Email       → Email
//   - X-User-Role        → Role
//   - X-User-Name        → Name

// Inyecta IUserContext en controllers vía DI
```

### IUserContext Interface

```csharp
public interface IUserContext
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    string Email { get; }
    string Role { get; }
    string Name { get; }
}
```

### Uso en Controllers

```csharp
public class UserController(
    IUserService service,
    IUserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // ✅ Validación de autenticación
        if (!userContext.IsAuthenticated)
            return Unauthorized(new { message = "User not authenticated" });

        // ✅ Verificar permisos de admin
        if (userContext.Role != "Admin")
            return Forbid();

        var users = await service.GetAllAsync();
        return Ok(users);
    }
}
```

### JWT Configuration

**appsettings.json:**

```json
{
  "Gateway": {
    "Secret": "VGhpc0lzQVNlY3JldEtleUZvckdhdGV3YXkyMDI0"
  },
  "JwtSettings": {
    "SecretKey": "your-jwt-secret-key-min-32-chars",
    "Issuer": "https://accessibility.company.com",
    "Audience": "https://accessibility.company.com",
    "ExpiryHours": 24
  }
}
```

**Generar Secrets:**

```powershell
# Generar JWT Secret Key
.\Generate-JwtSecretKey.ps1

# Validar configuración JWT
.\Validate-JwtConfig.ps1
```

### Flujo de Autenticación Completo

**1. Login (POST /api/auth/login)**

```
Client → Gateway → Users API
  1. Valida email/password contra DB
  2. Genera JWT token con claims (UserId, Email, Role)
  3. Crea sesión activa en DB
  4. Retorna token + datos de usuario
```

**2. Operaciones Protegidas**

```
Client (con JWT) → Gateway → Users API
  1. Gateway valida JWT y extrae claims
  2. Gateway agrega headers X-User-*
  3. UserContextMiddleware crea IUserContext
  4. Controller valida IsAuthenticated y permisos
  5. Ejecuta lógica de negocio
```

**3. Logout (POST /api/auth/logout)**

```
Client → Gateway → Users API
  1. Invalida token en sistema
  2. Cierra sesión activa en DB
  3. Retorna confirmación
```

## 🛠️ Scripts & Utilidades

### PowerShell Scripts

**1. Generate-JwtSecretKey.ps1**

Genera una clave secreta segura para JWT de forma automática.

```powershell
# Ejecutar script
.\Generate-JwtSecretKey.ps1

# Salida:
# ✅ JWT Secret Key generada exitosamente
# 🔑 Clave: AbCdEf12...GhIjKl34 (32+ caracteres)
# 📝 Agregar en appsettings.json -> JwtSettings:SecretKey
```

**Características:**

- Genera claves de 32+ caracteres automáticamente
- Usa RNGCryptoServiceProvider (cryptographically secure)
- Valida longitud mínima requerida
- Formato Base64 URL-safe

**2. Validate-JwtConfig.ps1**

Valida la configuración JWT en `appsettings.json` antes de deployment.

```powershell
# Ejecutar validación
.\Validate-JwtConfig.ps1

# Salida exitosa:
# ✅ JwtSettings:SecretKey existe
# ✅ Longitud: 64 caracteres (>= 32 requeridos)
# ✅ JwtSettings:Issuer configurado
# ✅ JwtSettings:Audience configurado
# ✅ JwtSettings:ExpiryHours configurado: 24
# ✅ Configuración JWT válida para producción

# Salida con errores:
# ❌ JwtSettings:SecretKey no encontrada
# ❌ SecretKey muy corta: 16 caracteres (mínimo 32)
# ⚠️ Considere ejecutar .\Generate-JwtSecretKey.ps1
```

**Validaciones:**

- ✅ Existencia de `JwtSettings:SecretKey`
- ✅ Longitud mínima (32 caracteres)
- ✅ Configuración de `Issuer` y `Audience`
- ✅ Valor de `ExpiryHours`
- ✅ Formato JSON válido

**3. init-test-databases.ps1 / .sh**

Inicializa bases de datos de prueba para testing local.

```powershell
# Windows
.\init-test-databases.ps1

# Linux/Mac
chmod +x init-test-databases.sh
./init-test-databases.sh
```

**Funcionalidad:**

- Crea contenedor MySQL para testing
- Ejecuta script `init-users-db.sql`
- Configura usuario y permisos
- Configura timezone de Ecuador (UTC-5)
- Verifica conexión antes de salir

**Variables de entorno requeridas:**

```env
MYSQL_TEST_ROOT_PASSWORD=test_root_pass
MYSQL_TEST_DATABASE=users_test_db
MYSQL_TEST_USER=test_user
MYSQL_TEST_PASSWORD=test_pass
TZ=America/Guayaquil
```

**4. manage-tests.ps1**

Script unificado para ejecutar tests con diferentes configuraciones.

```powershell
# Ejecutar todos los tests
.\manage-tests.ps1

# Tests con cobertura y reporte HTML
.\manage-tests.ps1 -GenerateCoverage -OpenReport

# Tests por tipo
.\manage-tests.ps1 -TestType Unit
.\manage-tests.ps1 -TestType Integration

# Ver solo resumen
.\manage-tests.ps1 -Summary
```

**Características:**

- Ejecuta xUnit con configuración personalizada
- Genera reportes de cobertura (Coverlet)
- Filtra por tipo (Unit/Integration/E2E)
- Exporta resultados a `TestResults/`
- Abre dashboard interactivo HTML

### C# Utilities

**DatabaseManager.cs**

Utilidad para gestión de esquema de base de datos en testing.

```csharp
public class DatabaseManager
{
    // Crear esquema completo desde cero
    public static async Task CreateSchemaAsync(string connectionString);

    // Limpiar todos los datos (mantiene estructura)
    public static async Task CleanDatabaseAsync(string connectionString);

    // Resetear base de datos (drop + recreate)
    public static async Task ResetDatabaseAsync(string connectionString);

    // Verificar conexión
    public static async Task<bool> CanConnectAsync(string connectionString);

    // Seed data de prueba
    public static async Task SeedTestDataAsync(string connectionString);
}
```

**Uso en tests:**

```csharp
[Fact]
public async Task Integration_CreateUser_ShouldSucceed()
{
    // Arrange: Limpiar estado previo
    await DatabaseManager.CleanDatabaseAsync(_connectionString);

    // Act: Ejecutar test
    var user = await _userService.CreateAsync(dto);

    // Assert
    Assert.NotNull(user);
    Assert.Equal(dto.Email, user.Email);
}
```

**DateTimeProvider.cs**

Servicio para manejo consistente de fechas en zona horaria de Ecuador.

```csharp
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo EcuadorTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.UtcNow, EcuadorTimeZone);

    public DateTime UtcNow => DateTime.UtcNow;
}
```

**Ventajas:**

- Centralización del manejo de fechas
- Testeable con mocks
- Independiente de configuración del servidor

### SQL Scripts

**init-users-db.sql**

Script de inicialización de base de datos con esquema completo.

```sql
-- Estructura:
-- 1. Creación de base de datos con UTF-8
-- 2. Tablas principales (users, preferences, sessions)
-- 3. Índices para performance
-- 4. Foreign keys y relaciones
-- 5. Usuario y permisos
-- 6. Configuración de timezone

-- Ejecutar manualmente:
mysql -u root -p < init-users-db.sql
```

**Tablas creadas:**

- `users` - Usuarios del sistema
- `preferences` - Preferencias de accesibilidad por usuario
- `sessions` - Sesiones activas

**Índices creados:**

- `idx_users_email` - Búsqueda rápida por email (UNIQUE)
- `idx_sessions_user` - Sesiones por usuario
- `idx_sessions_token` - Validación de tokens

## �🐳 Deployment

### Docker

```dockerfile
# Build image
docker build -t msusers-api:latest .

# Run standalone
docker run -d \
  --name msusers-api \
  -p 8081:8081 \
  -e ConnectionStrings__Default="server=mysql;database=usersdb;user=msuser;password=UsrApp2025SecurePass;DateTimeKind=Local" \
  -e JwtSettings__SecretKey="9b3e7ER@S^glvxPWKX8nN?DTqtrd%Yj!oVIfh+BG&piHwZz6ky4Q52MumOFA-Lc0" \
  -e Gateway__Secret="VGhpc0lzQVNlY3JldEtleUZvckdhdGV3YXkyMDI0" \
  msusers-api:latest
```

### Docker Compose

```yaml
version: "3.8"

services:
  mysql:
    image: mysql:8.4
    container_name: msusers-mysql
    environment:
      MYSQL_ROOT_PASSWORD: aF3MK0ZuWMHHXyX1ZwWjmKoS4baBAUgL
      MYSQL_DATABASE: usersdb
      MYSQL_USER: msuser
      MYSQL_PASSWORD: UsrApp2025SecurePass
      TZ: America/Guayaquil # Ecuador UTC-5
    command: --default-time-zone=-05:00
    ports:
      - "3307:3306"
    volumes:
      - msusers_mysql:/var/lib/mysql
      - ./init-users-db.sql:/docker-entrypoint-initdb.d/01-init-users.sql:ro
    networks:
      - default
      - accessibility-shared
    healthcheck:
      test:
        [
          "CMD",
          "mysqladmin",
          "ping",
          "-h",
          "localhost",
          "-paF3MK0ZuWMHHXyX1ZwWjmKoS4baBAUgL",
        ]
      interval: 10s
      timeout: 5s
      retries: 10

  api:
    image: msusers-api:latest
    container_name: msusers-api
    depends_on:
      mysql:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: "server=mysql;port=3306;database=usersdb;user=msuser;password=UsrApp2025SecurePass;TreatTinyAsBoolean=false;ConvertZeroDateTime=True;DateTimeKind=Local"
      JwtSettings__SecretKey: 9b3e7ER@S^glvxPWKX8nN?DTqtrd%Yj!oVIfh+BG&piHwZz6ky4Q52MumOFA-Lc0
      JwtSettings__Issuer: https://accessibility.company.com
      JwtSettings__Audience: https://accessibility.company.com
      JwtSettings__ExpiryHours: 24
      Gateway__Secret: VGhpc0lzQVNlY3JldEtleUZvckdhdGV3YXkyMDI0
    ports:
      - "8081:8081"
    networks:
      - default
      - accessibility-shared
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8081/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    labels:
      - "prometheus.scrape=true"
      - "prometheus.port=8081"
      - "prometheus.path=/metrics"
      - "service.name=users-api"
      - "service.version=1.0"

volumes:
  msusers_mysql:

networks:
  default:
    name: accessibility-ms-users_default
  accessibility-shared:
    external: true
    name: accessibility-shared
```

**Notas importantes:**

- **Red compartida:** `accessibility-shared` conecta todos los microservicios
- **Timezone MySQL:** Configurado en `-05:00` (Ecuador)
- **DateTimeKind=Local:** En ConnectionString para manejo correcto de fechas
- **Healthchecks:** MySQL espera estar healthy antes de iniciar API
- **Labels Prometheus:** Para monitoreo y métricas

## ⚙️ Configuración

### Variables de Entorno

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production|Development
ASPNETCORE_URLS=http://+:8081

# Base de Datos
ConnectionStrings__Default=server=localhost;port=3306;database=usersdb;user=msuser;password=UsrApp2025SecurePass;TreatTinyAsBoolean=false;ConvertZeroDateTime=True;DateTimeKind=Local
DB_ROOT_PASSWORD=aF3MK0ZuWMHHXyX1ZwWjmKoS4baBAUgL
DB_NAME=usersdb
DB_USER=msuser
DB_PASSWORD=UsrApp2025SecurePass
DB_PORT=3307

# MySQL Timezone (Ecuador UTC-5)
TZ=America/Guayaquil
MYSQL_TIMEZONE=-05:00

# JWT Configuration
JwtSettings__SecretKey=9b3e7ER@S^glvxPWKX8nN?DTqtrd%Yj!oVIfh+BG&piHwZz6ky4Q52MumOFA-Lc0
JwtSettings__Issuer=https://accessibility.company.com
JwtSettings__Audience=https://accessibility.company.com
JwtSettings__ExpiryHours=24

# Gateway Secret (comunicación entre servicios)
Gateway__Secret=VGhpc0lzQVNlY3JldEtleUZvckdhdGV3YXkyMDI0
GATEWAY_SECRET=VGhpc0lzQVNlY3JldEtleUZvckdhdGV3YXkyMDI0

# Email Configuration (para reset password)
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=your-email@gmail.com
Email__SmtpPassword=your-app-password

# Localization
DefaultLanguage=es
SupportedLanguages=es,en,pt

# Docker
API_HOST_PORT=8081

# Logging
Serilog__MinimumLevel=Information
Serilog__WriteTo__Console=true
```

### Configuración de Base de Datos

```sql
-- Crear base de datos con charset UTF-8
CREATE DATABASE usersdb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Ejecutar script de inicialización
SOURCE init-users-db.sql;

-- Verificar zona horaria (debe mostrar -05:00 para Ecuador)
SELECT @@global.time_zone, @@session.time_zone;
```

### Configuración de Zona Horaria

El microservicio implementa manejo de zona horaria para Ecuador (UTC-5):

**1. MySQL Timezone:**

```yaml
# docker-compose.yml
environment:
  TZ: America/Guayaquil
command: --default-time-zone=-05:00
```

**2. ConnectionString con DateTimeKind:**

```bash
ConnectionStrings__Default="...;DateTimeKind=Local"
```

**3. DateTimeProvider Service:**

```csharp
// Servicio personalizado para manejo de fechas en Ecuador
public class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo EcuadorTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.UtcNow,
        EcuadorTimeZone
    );
}
```

**4. Entity Framework ValueConverter:**

```csharp
// Todas las fechas se convierten automáticamente a Local
var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
    v => DateTime.SpecifyKind(v, DateTimeKind.Local),
    v => DateTime.SpecifyKind(v, DateTimeKind.Local)
);
```

**Resultado:** Todas las fechas se guardan y recuperan en hora de Ecuador (UTC-5).

## � Servicios Clave

### DateTimeProvider Service

Servicio personalizado para manejo consistente de zona horaria:

**Ubicación:** `Users.Application/Services/DateTimeProvider.cs`

```csharp
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo EcuadorTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(
        DateTime.UtcNow,
        EcuadorTimeZone
    );

    public DateTime UtcNow => DateTime.UtcNow;
}
```

**Registro en DI Container:**

```csharp
// Program.cs
builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
```

**Uso en servicios:**

```csharp
public class UserService
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        var user = new User
        {
            Email = dto.Email,
            CreatedAt = _dateTimeProvider.Now, // Hora de Ecuador
            UpdatedAt = _dateTimeProvider.Now
        };
        // ...
    }
}
```

**Ventajas:**

- ✅ Centralización del manejo de fechas
- ✅ Consistencia en toda la aplicación
- ✅ Facilita testing con mocks
- ✅ Independiente de la configuración del servidor
- ✅ Compatible con diferentes zonas horarias

## �🛠️ Stack Tecnológico

- **Runtime:** .NET 9.0
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core 9.0
- **Database:** MySQL 8.4
- **Authentication:** JWT Bearer
- **Timezone:** America/Guayaquil (Ecuador UTC-5)
- **Logging:** Serilog
- **Testing:** xUnit + Moq + FluentAssertions
- **Coverage:** Coverlet + ReportGenerator
- **Container:** Docker + Docker Compose
- **Networks:** Docker shared network (accessibility-shared)

## 📦 Servicios Relacionados

Este microservicio forma parte del ecosistema de accesibilidad:

- **Gateway (Port 8080):** Enrutamiento, rate limiting, circuit breaker
- **Analysis Service (Port 5002):** Análisis de accesibilidad WCAG
- **Reports Service (Port 5003):** Generación de reportes
- **Middleware (Port 3001):** Orquestación y lógica de negocio
- **UI (Port 5173):** Interfaz de usuario

**Red compartida:** Todos los servicios se conectan a través de `accessibility-shared` network.

## 📜 License

**Proprietary Software License v1.0**

Copyright (c) 2025 Geovanny Camacho. All rights reserved.

**IMPORTANT:** This software and associated documentation files (the "Software") are the exclusive property of Geovanny Camacho and are protected by copyright laws and international treaty provisions.

### TERMS AND CONDITIONS

1. **OWNERSHIP**: The Software is licensed, not sold. Geovanny Camacho retains all right, title, and interest in and to the Software, including all intellectual property rights.

2. **RESTRICTIONS**: You may NOT:

   - Copy, modify, or create derivative works of the Software
   - Distribute, transfer, sublicense, lease, lend, or rent the Software
   - Reverse engineer, decompile, or disassemble the Software
   - Remove or alter any proprietary notices or labels on the Software
   - Use the Software for any commercial purpose without explicit written permission
   - Share access credentials or allow unauthorized access to the Software

3. **CONFIDENTIALITY**: The Software contains trade secrets and confidential information. You agree to maintain the confidentiality of the Software and not disclose it to any third party.

4. **TERMINATION**: This license is effective until terminated. Your rights under this license will terminate automatically without notice if you fail to comply with any of its terms.

5. **NO WARRANTY**: THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.

6. **LIMITATION OF LIABILITY**: IN NO EVENT SHALL GEOVANNY CAMACHO BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

7. **GOVERNING LAW**: This license shall be governed by and construed in accordance with the laws of the jurisdiction in which Geovanny Camacho resides, without regard to its conflict of law provisions.

8. **ENTIRE AGREEMENT**: This license constitutes the entire agreement between you and Geovanny Camacho regarding the Software and supersedes all prior or contemporaneous understandings.

**FOR LICENSING INQUIRIES:**  
Geovanny Camacho  
Email: fgiocl@outlook.com

**By using this Software, you acknowledge that you have read this license, understand it, and agree to be bound by its terms and conditions.**

---

**Author:** Geovanny Camacho (fgiocl@outlook.com)  
**Last Update:** 05/11/2025
