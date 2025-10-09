#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Valida la configuración JWT entre microservicios y el Gateway.

.DESCRIPTION
    Este script verifica que los valores de JWT (SecretKey, Issuer, Audience) estén sincronizados
    correctamente entre un microservicio y el Gateway de la plataforma Accessibility.
    Es genérico y puede usarse con cualquier microservicio.

.PARAMETER MicroservicePath
    Ruta al directorio del microservicio (ej: c:\Git\accessibility-ms-users)
    Si no se especifica, usa el directorio actual

.PARAMETER MicroserviceName
    Nombre del microservicio para mensajes (ej: "Users", "Reports", "Analysis")
    Si no se especifica, intenta detectarlo del nombre del directorio

.PARAMETER GatewayPath
    Ruta al directorio del Gateway (ej: c:\Git\accessibility-gw)
    Por defecto busca en el directorio padre

.PARAMETER Environment
    Entorno a validar: Development, Production
    Por defecto: Development

.EXAMPLE
    .\Validate-JwtConfig.ps1
    Ejecuta la validación detectando automáticamente las rutas

.EXAMPLE
    .\Validate-JwtConfig.ps1 -MicroservicePath "c:\Git\accessibility-ms-reports"
    Valida el microservicio Reports contra el Gateway

.EXAMPLE
    .\Validate-JwtConfig.ps1 -MicroserviceName "Analysis" -Environment Production
    Valida con nombre personalizado y ambiente de producción

.NOTES
    Versión: 2.0 - Genérico
    Autor: Accessibility Team
    Compatible con: accessibility-ms-users, accessibility-ms-reports, accessibility-ms-analysis, accessibility-gw
#>

param(
    [Parameter()]
    [string]$MicroservicePath = "",
    
    [Parameter()]
    [string]$MicroserviceName = "",
    
    [Parameter()]
    [string]$GatewayPath = "",
    
    [Parameter()]
    [ValidateSet("Development", "Production")]
    [string]$Environment = "Development"
)

# Colores para output
$ErrorColor = "Red"
$SuccessColor = "Green"
$WarningColor = "Yellow"
$InfoColor = "Cyan"
$DetailColor = "Gray"

Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor $InfoColor
Write-Host "║       JWT Configuration Validator v2.0                    ║" -ForegroundColor $InfoColor
Write-Host "║       Accessibility Platform - Generic                    ║" -ForegroundColor $InfoColor
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor $InfoColor
Write-Host ""

# Auto-detectar rutas si no se especificaron
if ([string]::IsNullOrWhiteSpace($MicroservicePath)) {
    $MicroservicePath = Get-Location
    Write-Host "🔍 Usando directorio actual: $MicroservicePath" -ForegroundColor $InfoColor
}

# Auto-detectar nombre del microservicio
if ([string]::IsNullOrWhiteSpace($MicroserviceName)) {
    $dirName = Split-Path $MicroservicePath -Leaf
    if ($dirName -match "accessibility-ms-(\w+)") {
        $MicroserviceName = $Matches[1].ToUpper()[0] + $Matches[1].Substring(1).ToLower()
    } elseif ($dirName -match "accessibility-gw") {
        $MicroserviceName = "Gateway"
    } else {
        $MicroserviceName = $dirName
    }
    Write-Host "🔍 Microservicio detectado: $MicroserviceName" -ForegroundColor $InfoColor
}

# Auto-detectar Gateway si no se especificó
if ([string]::IsNullOrWhiteSpace($GatewayPath)) {
    $parentDir = Split-Path $MicroservicePath -Parent
    if ($parentDir) {
        $possibleGatewayPaths = @(
            (Join-Path $parentDir "accessibility-gw"),
            "c:\Git\accessibility-gw",
            "..\accessibility-gw"
        )
        
        foreach ($path in $possibleGatewayPaths) {
            if (Test-Path $path) {
                $GatewayPath = $path
                Write-Host "🔍 Gateway detectado: $GatewayPath" -ForegroundColor $InfoColor
                break
            }
        }
    }
    
    if ([string]::IsNullOrWhiteSpace($GatewayPath)) {
        Write-Host "⚠️  No se pudo detectar el Gateway automáticamente" -ForegroundColor $WarningColor
        Write-Host "   Usa: -GatewayPath 'c:\ruta\al\gateway'" -ForegroundColor $WarningColor
        exit 1
    }
}

Write-Host "📋 Ambiente: $Environment" -ForegroundColor $InfoColor
Write-Host ""

# Rutas de los proyectos (buscar en src/)
$microserviceProjectPath = Join-Path $MicroservicePath "src"
$gatewayProjectPath = Join-Path $GatewayPath "src" "Gateway"

# Buscar el proyecto .csproj del microservicio
$microserviceCsproj = Get-ChildItem -Path $microserviceProjectPath -Filter "*.Api.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $microserviceCsproj) {
    $microserviceCsproj = Get-ChildItem -Path $microserviceProjectPath -Filter "*.csproj" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($microserviceCsproj) {
    $microserviceProjectPath = $microserviceCsproj.DirectoryName
} else {
    Write-Host "❌ ERROR: No se encontró proyecto .csproj en: $microserviceProjectPath" -ForegroundColor $ErrorColor
    exit 1
}

$microserviceAppSettingsPath = Join-Path $microserviceProjectPath "appsettings.json"
$gatewayAppSettingsPath = if ($Environment -eq "Development") {
    Join-Path $gatewayProjectPath "appsettings.Development.json"
} else {
    Join-Path $gatewayProjectPath "appsettings.Production.json"
}

# Verificar que existan los directorios
if (-not (Test-Path $microserviceProjectPath)) {
    Write-Host "❌ ERROR: No se encontró el proyecto $MicroserviceName en: $microserviceProjectPath" -ForegroundColor $ErrorColor
    exit 1
}

if (-not (Test-Path $gatewayProjectPath)) {
    Write-Host "❌ ERROR: No se encontró el proyecto Gateway en: $gatewayProjectPath" -ForegroundColor $ErrorColor
    exit 1
}

# ============================================================================
# 1. VALIDAR MICROSERVICIO
# ============================================================================
Write-Host "┌───────────────────────────────────────────────────────────┐" -ForegroundColor $InfoColor
Write-Host "│ 1️⃣  Microservicio $MicroserviceName                           │" -ForegroundColor $InfoColor
Write-Host "└───────────────────────────────────────────────────────────┘" -ForegroundColor $InfoColor
Write-Host ""

# Leer User Secrets
Push-Location $microserviceProjectPath
try {
    $microserviceSecrets = dotnet user-secrets list 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ⚠️  User Secrets no inicializados" -ForegroundColor $WarningColor
        $microserviceSecretKey = $null
    } else {
        $microserviceSecretKeyLine = $microserviceSecrets | Select-String "JwtSettings:SecretKey"
        if ($microserviceSecretKeyLine) {
            $microserviceSecretKey = ($microserviceSecretKeyLine -split " = ")[1].Trim()
            Write-Host "   ✅ SecretKey configurado en User Secrets" -ForegroundColor $SuccessColor
            Write-Host "      Longitud: $($microserviceSecretKey.Length) caracteres" -ForegroundColor $DetailColor
        } else {
            Write-Host "   ❌ SecretKey NO encontrado en User Secrets" -ForegroundColor $ErrorColor
            $microserviceSecretKey = $null
        }
    }
} catch {
    Write-Host "   ❌ Error al leer User Secrets: $_" -ForegroundColor $ErrorColor
    $microserviceSecretKey = $null
} finally {
    Pop-Location
}

# Leer appsettings.json
if (Test-Path $microserviceAppSettingsPath) {
    $microserviceAppSettings = Get-Content $microserviceAppSettingsPath -Raw | ConvertFrom-Json
    $microserviceIssuer = $microserviceAppSettings.JwtSettings.Issuer
    $microserviceAudience = $microserviceAppSettings.JwtSettings.Audience
    $microserviceExpiryHours = $microserviceAppSettings.JwtSettings.ExpiryHours
    
    Write-Host "   Issuer:       $microserviceIssuer" -ForegroundColor $DetailColor
    Write-Host "   Audience:     $microserviceAudience" -ForegroundColor $DetailColor
    Write-Host "   ExpiryHours:  $microserviceExpiryHours" -ForegroundColor $DetailColor
} else {
    Write-Host "   ❌ No se encontró appsettings.json" -ForegroundColor $ErrorColor
    exit 1
}

Write-Host ""

# ============================================================================
# 2. VALIDAR GATEWAY
# ============================================================================
Write-Host "┌───────────────────────────────────────────────────────────┐" -ForegroundColor $InfoColor
Write-Host "│ 2️⃣  Gateway (accessibility-gw)                           │" -ForegroundColor $InfoColor
Write-Host "└───────────────────────────────────────────────────────────┘" -ForegroundColor $InfoColor
Write-Host ""

# Leer User Secrets
Push-Location $gatewayProjectPath
try {
    $gatewaySecrets = dotnet user-secrets list 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ⚠️  User Secrets no inicializados" -ForegroundColor $WarningColor
        $gatewaySecretKey = $null
    } else {
        $gatewaySecretKeyLine = $gatewaySecrets | Select-String "Jwt:SecretKey"
        if ($gatewaySecretKeyLine) {
            $gatewaySecretKey = ($gatewaySecretKeyLine -split " = ")[1].Trim()
            Write-Host "   ✅ SecretKey configurado en User Secrets" -ForegroundColor $SuccessColor
            Write-Host "      Longitud: $($gatewaySecretKey.Length) caracteres" -ForegroundColor $DetailColor
        } else {
            Write-Host "   ❌ SecretKey NO encontrado en User Secrets" -ForegroundColor $ErrorColor
            $gatewaySecretKey = $null
        }
    }
} catch {
    Write-Host "   ❌ Error al leer User Secrets: $_" -ForegroundColor $ErrorColor
    $gatewaySecretKey = $null
} finally {
    Pop-Location
}

# Leer appsettings.Development.json o Production.json
if (Test-Path $gatewayAppSettingsPath) {
    $gatewayAppSettings = Get-Content $gatewayAppSettingsPath -Raw | ConvertFrom-Json
    $gatewayIssuer = $gatewayAppSettings.Jwt.ValidIssuer
    $gatewayAudience = $gatewayAppSettings.Jwt.ValidAudience
    
    Write-Host "   ValidIssuer:   $gatewayIssuer" -ForegroundColor $DetailColor
    Write-Host "   ValidAudience: $gatewayAudience" -ForegroundColor $DetailColor
} else {
    Write-Host "   ❌ No se encontró appsettings.$Environment.json" -ForegroundColor $ErrorColor
    exit 1
}

Write-Host ""

# ============================================================================
# 3. VALIDACIÓN CRUZADA
# ============================================================================
Write-Host "┌───────────────────────────────────────────────────────────┐" -ForegroundColor $InfoColor
Write-Host "│ 3️⃣  Validación Cruzada                                   │" -ForegroundColor $InfoColor
Write-Host "└───────────────────────────────────────────────────────────┘" -ForegroundColor $InfoColor
Write-Host ""

$validationPassed = $true

# Validar SecretKeys
Write-Host "   🔑 SecretKey:" -ForegroundColor $InfoColor
if ($microserviceSecretKey -and $gatewaySecretKey) {
    if ($microserviceSecretKey -eq $gatewaySecretKey) {
        Write-Host "      ✅ COINCIDEN (ambos servicios usan la misma clave)" -ForegroundColor $SuccessColor
    } else {
        Write-Host "      ❌ NO COINCIDEN - Los tokens generados por $MicroserviceName no serán válidos en Gateway" -ForegroundColor $ErrorColor
        Write-Host "         $MicroserviceName (length: $($microserviceSecretKey.Length)): $($microserviceSecretKey.Substring(0, [Math]::Min(20, $microserviceSecretKey.Length)))..." -ForegroundColor $DetailColor
        Write-Host "         Gateway (length: $($gatewaySecretKey.Length)): $($gatewaySecretKey.Substring(0, [Math]::Min(20, $gatewaySecretKey.Length)))..." -ForegroundColor $DetailColor
        $validationPassed = $false
    }
} elseif (-not $microserviceSecretKey -and -not $gatewaySecretKey) {
    Write-Host "      ⚠️  Ninguno tiene SecretKey configurado" -ForegroundColor $WarningColor
    $validationPassed = $false
} elseif (-not $microserviceSecretKey) {
    Write-Host "      ❌ Falta SecretKey en $MicroserviceName" -ForegroundColor $ErrorColor
    $validationPassed = $false
} else {
    Write-Host "      ❌ Falta SecretKey en Gateway" -ForegroundColor $ErrorColor
    $validationPassed = $false
}
Write-Host ""

# Validar Issuer
Write-Host "   🏢 Issuer:" -ForegroundColor $InfoColor
if ($microserviceIssuer -eq $gatewayIssuer) {
    Write-Host "      ✅ COINCIDEN: '$microserviceIssuer'" -ForegroundColor $SuccessColor
} else {
    Write-Host "      ❌ NO COINCIDEN" -ForegroundColor $ErrorColor
    Write-Host "         $MicroserviceName : '$microserviceIssuer'" -ForegroundColor $DetailColor
    Write-Host "         Gateway: '$gatewayIssuer'" -ForegroundColor $DetailColor
    $validationPassed = $false
}
Write-Host ""

# Validar Audience
Write-Host "   👥 Audience:" -ForegroundColor $InfoColor
if ($microserviceAudience -eq $gatewayAudience) {
    Write-Host "      ✅ COINCIDEN: '$microserviceAudience'" -ForegroundColor $SuccessColor
} else {
    Write-Host "      ❌ NO COINCIDEN" -ForegroundColor $ErrorColor
    Write-Host "         $MicroserviceName : '$microserviceAudience'" -ForegroundColor $DetailColor
    Write-Host "         Gateway: '$gatewayAudience'" -ForegroundColor $DetailColor
    $validationPassed = $false
}
Write-Host ""

# ============================================================================
# 4. RESULTADO FINAL
# ============================================================================
Write-Host "┌───────────────────────────────────────────────────────────┐" -ForegroundColor $InfoColor
Write-Host "│ 4️⃣  Resultado Final                                      │" -ForegroundColor $InfoColor
Write-Host "└───────────────────────────────────────────────────────────┘" -ForegroundColor $InfoColor
Write-Host ""

if ($validationPassed) {
    Write-Host "   ✅ VALIDACIÓN EXITOSA" -ForegroundColor $SuccessColor
    Write-Host "   Todos los valores JWT están correctamente sincronizados." -ForegroundColor $SuccessColor
    Write-Host ""
    Write-Host "   Puedes proceder con:" -ForegroundColor $InfoColor
    Write-Host "   - Iniciar ambos servicios ($MicroserviceName y Gateway)" -ForegroundColor $DetailColor
    Write-Host "   - Realizar login a través del Gateway" -ForegroundColor $DetailColor
    Write-Host "   - El token generado será válido en el Gateway" -ForegroundColor $DetailColor
    Write-Host ""
    exit 0
} else {
    Write-Host "   ❌ VALIDACIÓN FALLIDA" -ForegroundColor $ErrorColor
    Write-Host "   Hay inconsistencias en la configuración JWT." -ForegroundColor $ErrorColor
    Write-Host ""
    Write-Host "   Acciones recomendadas:" -ForegroundColor $WarningColor
    Write-Host ""
    
    if ($microserviceSecretKey -ne $gatewaySecretKey) {
        Write-Host "   1. Sincronizar SecretKey:" -ForegroundColor $InfoColor
        Write-Host "      cd $microserviceProjectPath" -ForegroundColor $DetailColor
        Write-Host "      dotnet user-secrets set 'JwtSettings:SecretKey' '<tu-clave-segura>'" -ForegroundColor $DetailColor
        Write-Host "" -ForegroundColor $DetailColor
        Write-Host "      cd $gatewayProjectPath" -ForegroundColor $DetailColor
        Write-Host "      dotnet user-secrets set 'Jwt:SecretKey' '<la-misma-clave>'" -ForegroundColor $DetailColor
        Write-Host ""
    }
    
    if ($microserviceIssuer -ne $gatewayIssuer) {
        Write-Host "   2. Sincronizar Issuer en appsettings:" -ForegroundColor $InfoColor
        Write-Host "      - $MicroserviceName : appsettings.json -> JwtSettings.Issuer" -ForegroundColor $DetailColor
        Write-Host "      - Gateway: appsettings.$Environment.json -> Jwt.ValidIssuer" -ForegroundColor $DetailColor
        Write-Host ""
    }
    
    if ($microserviceAudience -ne $gatewayAudience) {
        Write-Host "   3. Sincronizar Audience en appsettings:" -ForegroundColor $InfoColor
        Write-Host "      - $MicroserviceName : appsettings.json -> JwtSettings.Audience" -ForegroundColor $DetailColor
        Write-Host "      - Gateway: appsettings.$Environment.json -> Jwt.ValidAudience" -ForegroundColor $DetailColor
        Write-Host ""
    }
    
    Write-Host "   💡 Usar script genérico: .\Generate-JwtSecretKey.ps1 -Output UserSecrets" -ForegroundColor $InfoColor
    Write-Host ""
    exit 1
}

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $InfoColor
Write-Host ""
