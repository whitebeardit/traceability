# Script de verificacao do Node.js e npm
# Execute: .\scripts\verify-nodejs.ps1

Write-Host "Verificando instalacao do Node.js e npm..." -ForegroundColor Cyan
Write-Host ""

$nodeInstalled = $false
$npmInstalled = $false

# Verificar Node.js
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Node.js instalado: $nodeVersion" -ForegroundColor Green
        $nodeInstalled = $true
    }
} catch {
    Write-Host "[ERRO] Node.js NAO esta instalado" -ForegroundColor Red
}

# Verificar npm
try {
    $npmVersion = npm --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] npm instalado: $npmVersion" -ForegroundColor Green
        $npmInstalled = $true
    }
} catch {
    Write-Host "[ERRO] npm NAO esta instalado" -ForegroundColor Red
}

Write-Host ""

if ($nodeInstalled -and $npmInstalled) {
    Write-Host "[OK] Ambiente Node.js configurado corretamente!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Proximos passos:" -ForegroundColor Cyan
    Write-Host "  1. Execute: npm install" -ForegroundColor Yellow
    Write-Host "  2. Isso instalara as dependencias do semantic-release" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "[ERRO] Node.js e/ou npm nao estao instalados" -ForegroundColor Red
    Write-Host ""
    Write-Host "Por favor, instale o Node.js:" -ForegroundColor Yellow
    Write-Host "  1. Acesse: https://nodejs.org" -ForegroundColor Cyan
    Write-Host "  2. Baixe a versao LTS" -ForegroundColor Cyan
    Write-Host "  3. Execute o instalador" -ForegroundColor Cyan
    Write-Host "  4. Reinicie o terminal e execute este script novamente" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Ou consulte: docs/setup-nodejs.md" -ForegroundColor Cyan
    exit 1
}

