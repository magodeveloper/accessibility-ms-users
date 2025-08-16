# accessibility-ms-users

Microservicio de gestión de usuarios y preferencias de accesibilidad, desarrollado en .NET 9 Minimal API, con integración a MySQL y soporte para despliegue en Docker.

## Características principales

- API RESTful para gestión de usuarios y preferencias de accesibilidad.
- Endpoints para registro, login, actualización y eliminación de usuarios por email.
- Gestión de preferencias WCAG, idioma, tema visual, formato de reporte, notificaciones y nivel de respuesta AI.
- Respuestas internacionalizadas (i18n) y manejo global de errores.
- Validación robusta con FluentValidation.
- Documentación OpenAPI/Swagger integrada.
- Pruebas de integración automatizadas con xUnit.
- Listo para despliegue en Docker y Docker Compose.

## Estructura del proyecto

```
.
├── docker-compose.yml
├── Dockerfile
├── .env.development
├── .env.production
├── README.md
└── src/
		├── Users.Api/           # API principal (Minimal API)
		├── Users.Application/   # DTOs, validadores y lógica de aplicación
		├── Users.Domain/        # Entidades y enums de dominio
		├── Users.Infrastructure/# DbContext y servicios de infraestructura
		└── Users.Tests/         # Pruebas de integración
```

## Variables de entorno

Configura los archivos `.env.development` y `.env.production` para tus entornos. Ejemplo:

```env
ASPNETCORE_ENVIRONMENT=Development
DB_NAME=msusers
DB_USER=msusers
DB_PASSWORD=msusers123
DB_PORT=3306
API_HOST_PORT=8080
```

## Uso con Docker Compose

```bash
# Desarrollo
docker compose --env-file .env.development up --build

# Producción
docker compose --env-file .env.production up --build
```

## Dockerización y despliegue

Este proyecto está preparado para ejecutarse fácilmente en contenedores Docker, tanto en desarrollo como en producción.

- **Dockerfile**: Define cómo construir la imagen de la API (compilación, dependencias, puertos expuestos).
- **docker-compose.yml**: Orquesta los servicios (API y MySQL), define variables de entorno, mapea puertos y gestiona dependencias.
- **.env.development / .env.production**: Archivos de variables de entorno para cada ambiente. Se referencian automáticamente en docker-compose.

### Flujo recomendado

1. Ajusta las variables en `.env.development` o `.env.production` según el entorno.
2. Ejecuta:
   ```sh
   docker compose --env-file .env.development up --build
   # o para producción
   docker compose --env-file .env.production up --build
   ```
3. Accede a la API en el puerto definido por `API_HOST_PORT` (por defecto 8080).

### Personalización del nombre de la imagen

Puedes personalizar el nombre de la imagen agregando la propiedad `image:` en el servicio `api` de tu `docker-compose.yml`:

```yaml
api:
  image: msusers-api:latest
  build:
    context: .
    dockerfile: ./Dockerfile
  # ...
```

Esto generará la imagen con ese nombre y etiqueta.

### Comandos útiles

- Parar y eliminar contenedores y volúmenes:
  ```sh
  docker compose down -v
  ```
- Ver logs de la API:
  ```sh
  docker compose logs -f api
  ```
- Limpiar imágenes sin usar:
  ```sh
  docker image prune
  ```

---

## Endpoints principales

- `POST   /api/v1/users-with-preferences`  
  Crea un usuario y sus preferencias por defecto en una sola llamada.

- `GET    /api/v1/preferences/by-user/{email}`  
  Obtiene las preferencias y datos del usuario por email.

- `PATCH  /api/v1/preferences/by-user/{email}`  
  Actualiza preferencias y datos del usuario por email.

- `DELETE /api/v1/users/by-email/{email}`  
  Elimina un usuario y sus preferencias por email.

- `POST   /api/v1/users/login`  
  Login de usuario, retorna token de sesión.

- `POST   /api/v1/users/reset-password`  
  Solicita o realiza reseteo de contraseña.

> Consulta la documentación Swagger en `/swagger` cuando la API esté corriendo en modo desarrollo.

## Pruebas

Ejecuta las pruebas de integración con:

````bash

## Endpoints principales

### POST /api/v1/users-with-preferences
Crea un usuario y sus preferencias por defecto.

**Payload ejemplo:**
```json
{
	"nickname": "jdoe",
	"name": "John",
	"lastname": "Doe",
	"email": "jdoe@email.com",
	"password": "Test1234!"
}
````

**Respuesta 201:**

```json
{
  "user": {
    "id": 1,
    "nickname": "jdoe",
    "name": "John",
    "lastname": "Doe",
    "email": "jdoe@email.com",
    "role": "user",
    "status": "active",
    "emailConfirmed": false,
    "lastLogin": null,
    "registrationDate": "2025-08-16T00:00:00Z",
    "createdAt": "2025-08-16T00:00:00Z",
    "updatedAt": "2025-08-16T00:00:00Z"
  },
  "preferences": {
    "wcagVersion": "2.1",
    "wcagLevel": "AA",
    "language": "es",
    "visualTheme": "light",
    "reportFormat": "pdf",
    "notificationsEnabled": true,
    "aiResponseLevel": "intermediate",
    "fontSize": 14
  }
}
```

### GET /api/v1/preferences/by-user/{email}

Obtiene las preferencias y datos del usuario por email.

**Respuesta 200:**

```json
{
  "user": {
    "id": 1,
    "nickname": "jdoe",
    "name": "John",
    "lastname": "Doe",
    "email": "jdoe@email.com",
    "role": "user",
    "status": "active"
  },
  "preferences": {
    "wcagVersion": "2.1",
    "wcagLevel": "AA",
    "language": "es",
    "visualTheme": "light",
    "reportFormat": "pdf",
    "notificationsEnabled": true,
    "aiResponseLevel": "intermediate",
    "fontSize": 14
  }
}
```

### PATCH /api/v1/preferences/by-user/{email}

Actualiza preferencias y datos del usuario por email.

**Payload ejemplo:**

```json
{
  "wcagVersion": "2.2",
  "wcagLevel": "AAA",
  "language": "en",
  "visualTheme": "dark",
  "reportFormat": "html",
  "notificationsEnabled": false,
  "aiResponseLevel": "detailed",
  "fontSize": 20,
  "nickname": "johnny",
  "name": "Johnny",
  "lastname": "Doe",
  "role": "admin",
  "status": "inactive",
  "emailConfirmed": true,
  "email": "johnny@email.com",
  "password": "NewPass123!"
}
```

**Respuesta 200:**

```json
{
  "user": {
    "id": 1,
    "nickname": "johnny",
    "name": "Johnny",
    "lastname": "Doe",
    "email": "johnny@email.com",
    "role": "admin",
    "status": "inactive"
  },
  "preferences": {
    "wcagVersion": "2.2",
    "wcagLevel": "AAA",
    "language": "en",
    "visualTheme": "dark",
    "reportFormat": "html",
    "notificationsEnabled": false,
    "aiResponseLevel": "detailed",
    "fontSize": 20
  }
}
```

### DELETE /api/v1/users/by-email/{email}

Elimina un usuario y sus preferencias por email.

**Respuesta 200:**

```json
{
  "message": "Usuario eliminado correctamente."
}
```

### POST /api/v1/users/login

Login de usuario, retorna token de sesión.

**Payload ejemplo:**

```json
{
  "email": "jdoe@email.com",
  "password": "Test1234!"
}
```

**Respuesta 200:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2025-08-17T00:00:00Z"
}
```

### POST /api/v1/users/reset-password

Solicita o realiza reseteo de contraseña.

**Payload ejemplo:**

```json
{
  "email": "jdoe@email.com"
}
```

**Respuesta 200:**

```json
{
  "message": "Si el email existe, se ha enviado un enlace de reseteo."
}
```

## Autenticación y manejo de errores

### Ejemplos de errores

**Error de validación (400):**

```json
{
  "error": "El email es obligatorio."
}
```

**No autorizado (401):**

```json
{
  "error": "No autorizado."
}
```

**No encontrado (404):**

```json
{
  "error": "Usuario no encontrado."
}
```

**Error interno (500):**

```json
{
  "error": "Ha ocurrido un error inesperado."
}
```

> Consulta la documentación Swagger en `/swagger` cuando la API esté corriendo en modo desarrollo.
> Este proyecto está preparado para integrarse fácilmente en pipelines de CI/CD modernos:

- **Build y test automáticos:**
  - Usa `dotnet build` y `dotnet test` para validar la solución en cada push o pull request.
- **Docker:**
  - El Dockerfile y docker-compose.yml permiten construir y desplegar el microservicio en cualquier entorno compatible con contenedores.
- **Variables de entorno:**
  - Utiliza archivos `.env` para separar configuraciones de desarrollo y producción.
- **Ejemplo de pasos en GitHub Actions:**
  ```yaml
  - name: Build
  	run: dotnet build
  - name: Test
  	run: dotnet test src/Users.Tests/Users.Tests.csproj
  - name: Docker Build
  	run: docker build -t msusers-api .
  - name: Docker Compose Up
  	run: docker compose --env-file .env.production up -d
  ```

---
