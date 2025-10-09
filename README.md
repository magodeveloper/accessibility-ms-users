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
docker build -t accessibility-users:latest .

# Run standalone
docker run -d \
  --name users-api \
  -p 8081:8081 \
  -e ConnectionStrings__UsersDb="Server=mysql;Database=users_db;..." \
  -e Jwt__Secret="your-secret-key" \
  accessibility-users:latest
```

### Docker Compose

```yaml
version: "3.8"

services:
  users-api:
    image: accessibility-users:latest
    ports:
      - "8081:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__UsersDb=Server=mysql-users;Database=users_db;Uid=root;Pwd=password
      - Jwt__Secret=your-jwt-secret
      - Jwt__Issuer=accessibility-platform
      - Jwt__Audience=accessibility-api
    depends_on:
      - mysql-users
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8081/health"]
      interval: 30s

  mysql-users:
    image: mysql:8.0
    ports:
      - "3307:3306"
    environment:
      - MYSQL_ROOT_PASSWORD=password
      - MYSQL_DATABASE=users_db
    volumes:
      - mysql-users-data:/var/lib/mysql
      - ./init-users-db.sql:/docker-entrypoint-initdb.d/init.sql

volumes:
  mysql-users-data:
```

## ⚙️ Configuración

### Variables de Entorno

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production|Development
ASPNETCORE_URLS=http://+:8081

# Base de Datos
ConnectionStrings__UsersDb=Server=localhost;Database=users_db;Uid=root;Pwd=password

# JWT Configuration
Jwt__Secret=your-super-secret-key-min-32-chars
Jwt__Issuer=accessibility-platform
Jwt__Audience=accessibility-api
Jwt__ExpirationMinutes=60

# Email Configuration (para reset password)
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=your-email@gmail.com
Email__SmtpPassword=your-app-password

# Localization
DefaultLanguage=es
SupportedLanguages=es,en,pt

# Logging
Serilog__MinimumLevel=Information
Serilog__WriteTo__Console=true
```

### Configuración de Base de Datos

```sql
-- Crear base de datos
CREATE DATABASE users_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Ejecutar script de inicialización
SOURCE init-users-db.sql;
```

## 🛠️ Stack Tecnológico

- **Runtime:** .NET 9.0
- **Framework:** ASP.NET Core Web API
- **ORM:** Entity Framework Core 9.0
- **Database:** MySQL 8.0+
- **Authentication:** JWT Bearer
- **Logging:** Serilog
- **Testing:** xUnit + Moq + FluentAssertions
- **Coverage:** Coverlet + ReportGenerator
- **Container:** Docker + Docker Compose

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
**Last Update:** 06/10/2025
