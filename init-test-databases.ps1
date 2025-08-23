# Script PowerShell para inicializar bases de datos de test
# Ejecutar antes de los tests

Write-Host "🔧 Inicializando bases de datos de test..." -ForegroundColor Yellow

# Configuración
$DB_HOST = "localhost"
$DB_PORT = "3306"
$DB_USER = "root"
$DB_PASSWORD = "eJ6RO5aYXQLLacA5azaqoOsW8feFFYkP"

# Función para ejecutar comandos MySQL
function Execute-SQL {
    param([string]$Query)
    
    try {
        & mysql -h $DB_HOST -P $DB_PORT -u $DB_USER -p$DB_PASSWORD -e $Query 2>$null
        return $true
    } catch {
        Write-Host "❌ Error ejecutando: $Query" -ForegroundColor Red
        return $false
    }
}

# Crear base de datos de usuarios de test
Write-Host "📊 Creando usersdb_test..." -ForegroundColor Green
Execute-SQL "DROP DATABASE IF EXISTS usersdb_test;"
Execute-SQL "CREATE DATABASE usersdb_test CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# Crear base de datos de análisis de test
Write-Host "📊 Creando analysisdb_test..." -ForegroundColor Green
Execute-SQL "DROP DATABASE IF EXISTS analysisdb_test;"
Execute-SQL "CREATE DATABASE analysisdb_test CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# Crear base de datos de reportes de test
Write-Host "📊 Creando reportsdb_test..." -ForegroundColor Green
Execute-SQL "DROP DATABASE IF EXISTS reportsdb_test;"
Execute-SQL "CREATE DATABASE reportsdb_test CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

# Crear usuario de test con permisos completos
Write-Host "👤 Creando usuario de test..." -ForegroundColor Green
Execute-SQL "DROP USER IF EXISTS 'testuser'@'%';"
Execute-SQL "CREATE USER 'testuser'@'%' IDENTIFIED BY 'TestApp2025SecurePass';"
Execute-SQL "GRANT ALL PRIVILEGES ON usersdb_test.* TO 'testuser'@'%';"
Execute-SQL "GRANT ALL PRIVILEGES ON analysisdb_test.* TO 'testuser'@'%';"
Execute-SQL "GRANT ALL PRIVILEGES ON reportsdb_test.* TO 'testuser'@'%';"
Execute-SQL "FLUSH PRIVILEGES;"

Write-Host ""
Write-Host "✅ Bases de datos de test inicializadas correctamente" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Ejecutar tests con:" -ForegroundColor Cyan
Write-Host "   dotnet test Users.sln --verbosity normal" -ForegroundColor White
