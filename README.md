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

**Total: 25 endpoints disponibles**

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

## 🐳 Deployment

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
