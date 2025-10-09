<#
.SYNOPSIS
    Generador seguro de JWT SecretKey para microservicios .NET
.DESCRIPTION
    Este script genera claves JWT criptográficamente seguras con diferentes
    niveles de complejidad y las prepara para usar en diferentes entornos.
    Es genérico y reutilizable en cualquier microservicio de la plataforma.
.PARAMETER Length
    Longitud de la clave (mínimo 32, recomendado 64)
.PARAMETER Type
    Tipo de clave: Alphanumeric, Special, Base64, Hex
.PARAMETER Environment
    Entorno objetivo: Development, Production, Testing
.PARAMETER Output
    Formato de salida: Console, File, Clipboard, UserSecrets
.PARAMETER ProjectPath
    Ruta relativa al proyecto .NET (ej: .\src\Api o .\src\Gateway)
    Si no se especifica, busca automáticamente un .csproj en ./src
.EXAMPLE
    .\Generate-JwtSecretKey.ps1
    Genera una clave alfanumérica de 64 caracteres
.EXAMPLE
    .\Generate-JwtSecretKey.ps1 -Type Special -Length 64 -Output Clipboard
    Genera una clave con caracteres especiales y la copia al portapapeles
.EXAMPLE
    .\Generate-JwtSecretKey.ps1 -Environment Production -Output File
    Genera una clave para producción y la guarda en archivo
.EXAMPLE
    .\Generate-JwtSecretKey.ps1 -Output UserSecrets -ProjectPath ".\src\Users.Api"
    Genera una clave y la configura directamente en User Secrets del proyecto especificado
.EXAMPLE
    .\Generate-JwtSecretKey.ps1 -Output UserSecrets
    Genera una clave y detecta automáticamente el proyecto en ./src
.NOTES
    Versión: 2.0 - Genérico
    Compatible con: accessibility-ms-users, accessibility-ms-reports, accessibility-ms-analysis, accessibility-gw
#>

param(
    [Parameter()]
    [ValidateRange(32, 256)]
    [int]$Length = 64,
    
    [Parameter()]
    [ValidateSet("Alphanumeric", "Special", "Base64", "Hex")]
    [string]$Type = "Alphanumeric",
    
    [Parameter()]
    [ValidateSet("Development", "Production", "Testing")]
    [string]$Environment = "Production",
    
    [Parameter()]
    [ValidateSet("Console", "File", "Clipboard", "UserSecrets", "All")]
    [string]$Output = "Console",
    
    [Parameter()]
    [string]$ProjectPath = "",
    
    [Parameter()]
    [switch]$Validate,
    
    [Parameter()]
    [switch]$ShowStatistics
)

# Configuración de colores
$ErrorActionPreference = "Stop"
$Colors = @{
    Header    = "Cyan"
    Success   = "Green"
    Warning   = "Yellow"
    Error     = "Red"
    Info      = "White"
    Highlight = "Magenta"
}

# Auto-detectar proyecto si no se especificó
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    Write-Host "🔍 Detectando proyecto automáticamente..." -ForegroundColor Cyan
    
    # Buscar en ./src o directorio actual
    $possiblePaths = @(".\src", ".")
    $foundProject = $null
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $csproj = Get-ChildItem -Path $path -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($csproj) {
                $foundProject = $csproj.DirectoryName
                break
            }
        }
    }
    
    if ($foundProject) {
        $ProjectPath = $foundProject
        Write-Host "✅ Proyecto detectado: $ProjectPath" -ForegroundColor Green
    } else {
        $ProjectPath = ".\src"
        Write-Host "⚠️  No se detectó proyecto, usando ruta por defecto: $ProjectPath" -ForegroundColor Yellow
    }
}

# Configuración de colores
$ErrorActionPreference = "Stop"
$Colors = @{
    Header    = "Cyan"
    Success   = "Green"
    Warning   = "Yellow"
    Error     = "Red"
    Info      = "White"
    Highlight = "Magenta"
}

function Write-ColorMessage {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Header {
    param([string]$Title)
    $border = "=" * 70
    Write-Host ""
    Write-ColorMessage $border $Colors.Header
    Write-ColorMessage "  $Title" $Colors.Header
    Write-ColorMessage $border $Colors.Header
    Write-Host ""
}

function Get-Entropy {
    param([string]$Key)
    
    $charSet = @{}
    foreach ($char in $Key.ToCharArray()) {
        if ($charSet.ContainsKey($char)) {
            $charSet[$char]++
        }
        else {
            $charSet[$char] = 1
        }
    }
    
    $entropy = 0
    $length = $Key.Length
    foreach ($count in $charSet.Values) {
        $probability = $count / $length
        $entropy -= $probability * [Math]::Log($probability, 2)
    }
    
    return [Math]::Round($entropy, 2)
}

function Test-KeyStrength {
    param([string]$Key)
    
    $length = $Key.Length
    $hasLower = $Key -cmatch '[a-z]'
    $hasUpper = $Key -cmatch '[A-Z]'
    $hasDigit = $Key -cmatch '\d'
    $hasSpecial = $Key -match '[^a-zA-Z0-9]'
    $entropy = Get-Entropy -Key $Key
    
    $score = 0
    $score += if ($length -ge 32) { 20 } else { 0 }
    $score += if ($length -ge 64) { 10 } else { 0 }
    $score += if ($hasLower) { 15 } else { 0 }
    $score += if ($hasUpper) { 15 } else { 0 }
    $score += if ($hasDigit) { 15 } else { 0 }
    $score += if ($hasSpecial) { 25 } else { 0 }
    
    $strength = switch ($score) {
        { $_ -ge 90 } { "Excelente" }
        { $_ -ge 70 } { "Muy Buena" }
        { $_ -ge 50 } { "Buena" }
        { $_ -ge 30 } { "Regular" }
        default { "Débil" }
    }
    
    return [PSCustomObject]@{
        Length      = $length
        HasLowercase = $hasLower
        HasUppercase = $hasUpper
        HasDigits   = $hasDigit
        HasSpecial  = $hasSpecial
        Entropy     = $entropy
        Score       = $score
        Strength    = $strength
        IsValid     = $length -ge 32
    }
}

function New-JwtSecretKey {
    param(
        [int]$Length,
        [string]$Type
    )
    
    switch ($Type) {
        "Alphanumeric" {
            # 0-9, A-Z, a-z
            $chars = (48..57) + (65..90) + (97..122)
            $key = -join ($chars | Get-Random -Count $Length | ForEach-Object { [char]$_ })
        }
        "Special" {
            # Alfanumérico + ! # % & * + - = ? @ ^
            $chars = (48..57) + (65..90) + (97..122) + (33, 35, 37, 38, 42, 43, 45, 61, 63, 64, 94)
            $key = -join ($chars | Get-Random -Count $Length | ForEach-Object { [char]$_ })
        }
        "Base64" {
            # Generar bytes aleatorios y convertir a Base64
            $bytes = 1..[Math]::Ceiling($Length * 0.75) | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }
            $key = [Convert]::ToBase64String($bytes)
            $key = $key.Substring(0, [Math]::Min($Length, $key.Length))
        }
        "Hex" {
            # Hexadecimal
            $bytes = 1..[Math]::Ceiling($Length / 2) | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }
            $key = ($bytes | ForEach-Object { $_.ToString("X2") }) -join ''
            $key = $key.Substring(0, [Math]::Min($Length, $key.Length))
        }
    }
    
    return $key
}

function Save-ToFile {
    param(
        [string]$Key,
        [string]$Environment
    )
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "jwt-secret-$Environment-$timestamp.txt"
    
    $content = @"
# JWT Secret Key for $Environment
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# Length: $($Key.Length) characters
# 
# IMPORTANTE: 
# - NO commitear este archivo al repositorio
# - Agregar '$filename' a .gitignore
# - Guardar en lugar seguro (Azure Key Vault recomendado)
# - Eliminar este archivo después de configurar en el entorno
#
# Para usar en .NET User Secrets:
# dotnet user-secrets set "JwtSettings:SecretKey" "$Key"
#
# Para usar en Docker/Docker Compose (.env):
# JWT_SECRET_KEY=$Key
#
# Para usar en Azure App Service:
# JwtSettings__SecretKey=$Key
#
# ============================================================

$Key

# ============================================================
"@
    
    $content | Out-File -FilePath $filename -Encoding UTF8
    Write-ColorMessage "✅ Clave guardada en: $filename" $Colors.Success
    Write-ColorMessage "⚠️  RECUERDA: Agregar '$filename' a .gitignore" $Colors.Warning
}

function Copy-ToClipboard {
    param([string]$Key)
    
    try {
        Set-Clipboard -Value $Key
        Write-ColorMessage "✅ Clave copiada al portapapeles" $Colors.Success
    }
    catch {
        Write-ColorMessage "❌ Error al copiar al portapapeles: $_" $Colors.Error
    }
}

function Set-UserSecrets {
    param(
        [string]$Key,
        [string]$ProjectPath
    )
    
    if (-not (Test-Path $ProjectPath)) {
        Write-ColorMessage "❌ Ruta del proyecto no encontrada: $ProjectPath" $Colors.Error
        return
    }
    
    Push-Location $ProjectPath
    
    try {
        # Verificar si user secrets está inicializado
        $csproj = Get-ChildItem -Filter "*.csproj" | Select-Object -First 1
        if (-not $csproj) {
            Write-ColorMessage "❌ No se encontró archivo .csproj en $ProjectPath" $Colors.Error
            return
        }
        
        $content = Get-Content $csproj.FullName -Raw
        if ($content -notmatch '<UserSecretsId>') {
            Write-ColorMessage "⚠️  Inicializando User Secrets..." $Colors.Warning
            dotnet user-secrets init
        }
        
        # Configurar el secret
        dotnet user-secrets set "JwtSettings:SecretKey" $Key
        Write-ColorMessage "✅ Clave configurada en User Secrets" $Colors.Success
        
        # Mostrar lista de secrets
        Write-ColorMessage "`nSecrets configurados:" $Colors.Info
        dotnet user-secrets list
    }
    catch {
        Write-ColorMessage "❌ Error configurando User Secrets: $_" $Colors.Error
    }
    finally {
        Pop-Location
    }
}

function Show-Usage {
    param([string]$Key, [string]$Environment)
    
    Write-ColorMessage "`n📋 INSTRUCCIONES DE USO" $Colors.Highlight
    Write-ColorMessage ("=" * 70) $Colors.Info
    
    Write-ColorMessage "`n1️⃣  User Secrets (.NET - Desarrollo Local):" $Colors.Highlight
    Write-ColorMessage "   cd <ruta-al-proyecto>  # Ej: src/Users.Api, src/Gateway" $Colors.Info
    Write-ColorMessage "   dotnet user-secrets set `"JwtSettings:SecretKey`" `"$Key`"" $Colors.Info
    Write-ColorMessage "   # O para Gateway:" $Colors.Info
    Write-ColorMessage "   dotnet user-secrets set `"Jwt:SecretKey`" `"$Key`"" $Colors.Info
    
    Write-ColorMessage "`n2️⃣  Docker Compose (.env file):" $Colors.Highlight
    Write-ColorMessage "   # Agregar a .env.$Environment" $Colors.Info
    Write-ColorMessage "   JWT_SECRET_KEY=$Key" $Colors.Info
    Write-ColorMessage "   # O para microservicios:" $Colors.Info
    Write-ColorMessage "   JwtSettings__SecretKey=$Key" $Colors.Info
    
    Write-ColorMessage "`n3️⃣  appsettings.$Environment.json:" $Colors.Highlight
    Write-ColorMessage @"
   {
     "JwtSettings": {
       "SecretKey": "$Key"
     }
   }
"@ $Colors.Info
    
    Write-ColorMessage "`n4️⃣  Azure App Service:" $Colors.Highlight
    Write-ColorMessage "   az webapp config appsettings set \\" $Colors.Info
    Write-ColorMessage "     --name mi-app-service \\" $Colors.Info
    Write-ColorMessage "     --resource-group mi-rg \\" $Colors.Info
    Write-ColorMessage "     --settings JwtSettings__SecretKey=`"$Key`"" $Colors.Info
    
    Write-ColorMessage "`n5️⃣  Azure Key Vault (Recomendado para Producción):" $Colors.Highlight
    Write-ColorMessage "   az keyvault secret set \\" $Colors.Info
    Write-ColorMessage "     --vault-name mi-keyvault \\" $Colors.Info
    Write-ColorMessage "     --name JwtSecretKey \\" $Colors.Info
    Write-ColorMessage "     --value `"$Key`"" $Colors.Info
    
    Write-ColorMessage "`n6️⃣  Kubernetes Secret:" $Colors.Highlight
    Write-ColorMessage "   kubectl create secret generic users-api-secrets \\" $Colors.Info
    Write-ColorMessage "     --from-literal=jwt-secret-key=`"$Key`"" $Colors.Info
    
    Write-ColorMessage "`n" $Colors.Info
}

# ============================================================
# MAIN SCRIPT
# ============================================================

Write-Header "🔐 Generador de JWT SecretKey - Accessibility Platform"

Write-ColorMessage "📋 Configuración:" $Colors.Info
Write-ColorMessage "   Longitud: $Length caracteres" $Colors.Info
Write-ColorMessage "   Tipo: $Type" $Colors.Info
Write-ColorMessage "   Entorno: $Environment" $Colors.Info
Write-ColorMessage "   Salida: $Output" $Colors.Info
if ($Output -eq "UserSecrets" -or $Output -eq "All") {
    Write-ColorMessage "   Proyecto: $ProjectPath" $Colors.Info
}
Write-Host ""

# Generar la clave
Write-ColorMessage "🔄 Generando clave criptográficamente segura..." $Colors.Info
$secretKey = New-JwtSecretKey -Length $Length -Type $Type
Write-ColorMessage "✅ Clave generada exitosamente" $Colors.Success
Write-Host ""

# Validar la clave
$validation = Test-KeyStrength -Key $secretKey

# Mostrar resultados
Write-ColorMessage "🔑 CLAVE GENERADA:" $Colors.Highlight
Write-ColorMessage ("=" * 70) $Colors.Info
Write-Host ""
Write-ColorMessage $secretKey $Colors.Success
Write-Host ""
Write-ColorMessage ("=" * 70) $Colors.Info

if ($Validate -or $ShowStatistics) {
    Write-Host ""
    Write-ColorMessage "📊 ANÁLISIS DE SEGURIDAD:" $Colors.Highlight
    Write-ColorMessage ("=" * 70) $Colors.Info
    Write-ColorMessage "   Longitud: $($validation.Length) caracteres" $Colors.Info
    Write-ColorMessage "   Minúsculas: $(if ($validation.HasLowercase) { '✅' } else { '❌' })" $Colors.Info
    Write-ColorMessage "   Mayúsculas: $(if ($validation.HasUppercase) { '✅' } else { '❌' })" $Colors.Info
    Write-ColorMessage "   Dígitos: $(if ($validation.HasDigits) { '✅' } else { '❌' })" $Colors.Info
    Write-ColorMessage "   Caracteres especiales: $(if ($validation.HasSpecial) { '✅' } else { '❌' })" $Colors.Info
    Write-ColorMessage "   Entropía: $($validation.Entropy) bits/char" $Colors.Info
    Write-ColorMessage "   Puntuación: $($validation.Score)/100" $Colors.Info
    
    $strengthColor = switch ($validation.Strength) {
        "Excelente" { "Green" }
        "Muy Buena" { "Green" }
        "Buena" { "White" }
        "Regular" { "Yellow" }
        default { "Red" }
    }
    Write-ColorMessage "   Fortaleza: $($validation.Strength)" $strengthColor
    Write-ColorMessage "   Válida para JWT: $(if ($validation.IsValid) { '✅ SÍ' } else { '❌ NO' })" $(if ($validation.IsValid) { "Green" } else { "Red" })
    
    if (-not $validation.IsValid) {
        Write-ColorMessage "`n⚠️  ADVERTENCIA: La clave es menor a 32 caracteres (no recomendada)" $Colors.Warning
    }
}

# Procesar salida
Write-Host ""
switch ($Output) {
    "Console" {
        # Ya se mostró arriba
        Show-Usage -Key $secretKey -Environment $Environment
    }
    "File" {
        Save-ToFile -Key $secretKey -Environment $Environment
        Show-Usage -Key $secretKey -Environment $Environment
    }
    "Clipboard" {
        Copy-ToClipboard -Key $secretKey
        Show-Usage -Key $secretKey -Environment $Environment
    }
    "UserSecrets" {
        Set-UserSecrets -Key $secretKey -ProjectPath $ProjectPath
        Show-Usage -Key $secretKey -Environment $Environment
    }
    "All" {
        Save-ToFile -Key $secretKey -Environment $Environment
        Copy-ToClipboard -Key $secretKey
        Set-UserSecrets -Key $secretKey -ProjectPath $ProjectPath
        Show-Usage -Key $secretKey -Environment $Environment
    }
}

# Advertencias de seguridad
Write-Host ""
Write-ColorMessage "⚠️  IMPORTANTE - SEGURIDAD:" $Colors.Warning
Write-ColorMessage ("=" * 70) $Colors.Warning
Write-ColorMessage "   • NO commitear esta clave al repositorio" $Colors.Warning
Write-ColorMessage "   • NO compartir por email, chat o medios inseguros" $Colors.Warning
Write-ColorMessage "   • Usar Azure Key Vault para producción" $Colors.Warning
Write-ColorMessage "   • Rotar claves cada 3-6 meses" $Colors.Warning
Write-ColorMessage "   • Eliminar archivos temporales después de configurar" $Colors.Warning
Write-Host ""

Write-ColorMessage "✅ Proceso completado exitosamente" $Colors.Success
Write-Host ""
