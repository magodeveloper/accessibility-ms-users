# accessibility-ms-users

Microservicio de gestión de usuarios y preferencias de accesibilidad, desarrollado en .NET 9 con controladores tradicionales, integración a MySQL y soporte para despliegue en Docker.

**Novedades recientes:**

- Todas las respuestas y errores están internacionalizadas (i18n) según la cabecera `Accept-Language`.
- Todas las respuestas usan DTOs para evitar ciclos y exponer solo los datos necesarios.
- El campo `wcagVersion` es siempre string (no enum).
- El endpoint de login retorna el usuario y sus preferencias asociadas.
- Nuevo endpoint: `DELETE /api/v1/sessions/by-user/{userId}` para eliminar todas las sesiones de un usuario.
- **🆕 Nuevo endpoint CRÍTICO: `DELETE /api/v1/users/all-data`** para eliminar TODOS los registros de usuarios, preferencias y sesiones.
- Rutas desambiguadas y robustas.
- Pruebas de integración completas y actualizadas.

## Características principales

- API RESTful para gestión de usuarios, sesiones y preferencias de accesibilidad.
- Endpoints para registro, login, actualización y eliminación de usuarios por email.
- **Método de limpieza total**: Endpoint para eliminar todos los datos (desarrollo y testing).
- Gestión de preferencias WCAG (como string), idioma, tema visual, formato de reporte, notificaciones y nivel de respuesta AI.
- Respuestas internacionalizadas (i18n) y manejo global de errores. El idioma se detecta automáticamente por la cabecera `Accept-Language`.
- Uso de DTOs para todas las respuestas (sin ciclos de entidades).
- Validación robusta con FluentValidation.
- Documentación OpenAPI/Swagger integrada.
- Pruebas de integración automatizadas con xUnit (cubre todos los endpoints principales).
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
ASPNETCORE_URLS=http://+:8081
DB_NAME=msusers
DB_USER=msusers
DB_PASSWORD=msusers123
API_HOST_PORT=8081
```

> **Nota:** No es necesario definir `DB_HOST` ni `DB_PORT` en los archivos `.env`, ya que la comunicación interna entre contenedores Docker utiliza el nombre del servicio (`mysql`) y el puerto por defecto (`3306`). La cadena de conexión ya está configurada correctamente en `docker-compose.yml`.

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
- **Limpiar base de datos (desarrollo/testing)**:
  ```sh
  curl -X DELETE http://localhost:8081/api/v1/users/all-data
  ```

---

## Endpoints principales

- `POST   /api/v1/users-with-preferences`  
  Crea un usuario y sus preferencias por defecto en una sola llamada.
- `DELETE /api/v1/users/by-email/{email}`  
  Elimina un usuario y sus preferencias por email.
- `POST   /api/v1/auth/login`  
  Login de usuario, retorna token de sesión **y ahora también el usuario y sus preferencias**.
- `POST   /api/v1/auth/logout`  
  Cierra la sesión del usuario.
- `DELETE /api/v1/sessions/by-user/{userId}`  
  Elimina todas las sesiones activas de un usuario.
- `GET    /api/v1/preferences/by-user/{email}`  
  Obtiene las preferencias de un usuario por email.
- `POST   /api/v1/preferences`  
  Crea preferencias para un usuario existente.
- `PATCH  /api/v1/preferences/{id}`  
  Actualiza parcialmente las preferencias.
- **⚠️ `DELETE /api/v1/users/all-data`**  
  **ELIMINA TODOS los registros de usuarios, preferencias y sesiones (IRREVERSIBLE)**.

> Consulta la documentación Swagger en `/swagger` cuando la API esté corriendo en modo desarrollo.

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
```

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
    "id": 1,
    "userId": 1,
    "wcagVersion": "2.1",
    "wcagLevel": "AA",
    "language": "es",
    "visualTheme": "light",
    "reportFormat": "pdf",
    "notificationsEnabled": true,
    "aiResponseLevel": "intermediate",
    "fontSize": 14,
    "createdAt": "2025-08-16T00:00:00Z",
    "updatedAt": "2025-08-16T00:00:00Z"
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

### POST /api/v1/auth/login

Login de usuario. Ahora retorna:

- token de sesión,
- usuario autenticado,
- preferencias asociadas.

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
  "expiresAt": "2025-08-17T00:00:00Z",
  "user": {
    "id": 1,
    "nickname": "jdoe",
    "name": "John",
    "lastname": "Doe",
    "email": "jdoe@email.com",
    "role": "user",
    "status": "active",
    "emailConfirmed": false
  },
  "preferences": {
    "id": 1,
    "userId": 1,
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

### DELETE /api/v1/sessions/by-user/{userId}

Elimina todas las sesiones activas de un usuario por su ID.

**Respuesta 200:**

```json
{
  "message": "Sesiones eliminadas correctamente."
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

### ⚠️ DELETE /api/v1/users/all-data

**OPERACIÓN CRÍTICA**: Elimina TODOS los registros de las tablas `USERS`, `PREFERENCES` y `SESSIONS`.

> **ADVERTENCIA**: Esta operación es **IRREVERSIBLE** y borra toda la información de la base de datos.

**Sin parámetros requeridos**

**Respuesta 200 (Éxito):**

```json
{
  "message": "Todos los datos (usuarios, preferencias y sesiones) han sido eliminados exitosamente. Base de datos limpia."
}
```

**Respuesta 500 (Error):**

```json
{
  "error": "Error al eliminar todos los datos. Operación cancelada."
}
```

#### Orden de eliminación:

1. **SESSIONS** (elimina dependencias de usuarios)
2. **PREFERENCES** (elimina dependencias de usuarios)
3. **USERS** (tabla principal)
4. **Reset AUTO_INCREMENT** (resetea IDs a 1)

#### Casos de uso recomendados:

✅ **Entornos de desarrollo** - Limpiar datos de prueba  
✅ **Testing automatizado** - Reset de base de datos entre tests  
✅ **Demos y talleres** - Volver a estado inicial

❌ **Entornos de producción** - NO recomendado sin medidas adicionales

#### Ejemplo de uso:

```bash
# cURL
curl -X DELETE http://localhost:8081/api/v1/users/all-data

# PowerShell
Invoke-RestMethod -Uri "http://localhost:8081/api/v1/users/all-data" -Method Delete
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

## Pruebas

El proyecto incluye pruebas de integración automatizadas para todos los endpoints principales. Para ejecutarlas:

```bash
dotnet test
```

Las pruebas cubren:

- Registro y login de usuario (incluyendo preferencias)
- CRUD de usuarios y preferencias
- CRUD de sesiones (incluyendo borrado por usuario)
- **Eliminación masiva de todos los datos** (DELETE /api/v1/users/all-data)
- Validación de errores y respuestas internacionalizadas

**Resultado esperado**: `6/6 tests passing`

## Notas adicionales

- Todas las respuestas de error y éxito están internacionalizadas (i18n).
- El campo `wcagVersion` es string en todos los endpoints y la base de datos.
- Todas las respuestas usan DTOs para evitar ciclos y exponer solo los datos necesarios.
- El endpoint de login retorna el usuario y sus preferencias asociadas.
- El endpoint para eliminar sesiones por usuario es `/api/v1/sessions/by-user/{userId}`.
- **⚠️ NUEVO**: Endpoint `/api/v1/users/all-data` para eliminación masiva (usar con precaución).
- El proyecto está listo para CI/CD y despliegue en Docker.
- Si usas frontend, asegúrate de configurar correctamente CORS en el backend.

### Consideraciones de seguridad para producción

Si planeas usar el endpoint `DELETE /api/v1/users/all-data` en producción, considera:

- Implementar autenticación y autorización (roles específicos)
- Agregar confirmación doble (headers especiales)
- Implementar logging de auditoría
- Crear respaldos automáticos antes de la eliminación

---
