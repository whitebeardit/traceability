# Script para corrigir o PATH do PowerShell após instalação do Node.js
# Execute: .\scripts\fix-powershell-path.ps1

Write-Host "Diagnostico do PATH do PowerShell..." -ForegroundColor Cyan
Write-Host ""

# Verificar se Node.js esta no PATH do sistema
$machinePath = [System.Environment]::GetEnvironmentVariable("Path","Machine")
$userPath = [System.Environment]::GetEnvironmentVariable("Path","User")

Write-Host "PATH do Sistema (Machine):" -ForegroundColor Yellow
if ($machinePath -match "nodejs") {
    Write-Host "  [OK] Node.js encontrado no PATH do sistema" -ForegroundColor Green
    $machinePath -split ';' | Where-Object { $_ -match "nodejs" } | ForEach-Object {
        Write-Host "    - $_" -ForegroundColor Gray
    }
} else {
    Write-Host "  [ERRO] Node.js NAO encontrado no PATH do sistema" -ForegroundColor Red
}

Write-Host ""
Write-Host "PATH do Usuario (User):" -ForegroundColor Yellow
if ($userPath -match "nodejs") {
    Write-Host "  [OK] Node.js encontrado no PATH do usuario" -ForegroundColor Green
    $userPath -split ';' | Where-Object { $_ -match "nodejs" } | ForEach-Object {
        Write-Host "    - $_" -ForegroundColor Gray
    }
} else {
    Write-Host "  [INFO] Node.js nao esta no PATH do usuario (normal se estiver no PATH do sistema)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "PATH da sessao atual:" -ForegroundColor Yellow
if ($env:Path -match "nodejs") {
    Write-Host "  [OK] Node.js encontrado no PATH da sessao" -ForegroundColor Green
    $env:Path -split ';' | Where-Object { $_ -match "nodejs" } | ForEach-Object {
        Write-Host "    - $_" -ForegroundColor Gray
    }
} else {
    Write-Host "  [ERRO] Node.js NAO encontrado no PATH da sessao atual" -ForegroundColor Red
    Write-Host "  Isso significa que o terminal precisa ser reiniciado ou o PATH precisa ser recarregado" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Testando comandos:" -ForegroundColor Yellow

# Recarregar PATH
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# Testar node
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] node --version: $nodeVersion" -ForegroundColor Green
    } else {
        Write-Host "  [ERRO] node nao funciona" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERRO] node nao encontrado" -ForegroundColor Red
}

# Testar npm
try {
    $npmVersion = npm --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] npm --version: $npmVersion" -ForegroundColor Green
    } else {
        Write-Host "  [ERRO] npm nao funciona" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERRO] npm nao encontrado" -ForegroundColor Red
}

Write-Host ""
Write-Host "Solucao:" -ForegroundColor Cyan
Write-Host "  1. Feche e reabra o PowerShell/terminal" -ForegroundColor Yellow
Write-Host "  2. Ou execute este comando no inicio de cada sessao:" -ForegroundColor Yellow
Write-Host "     `$env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Ou adicione ao seu perfil do PowerShell:" -ForegroundColor Yellow
Write-Host "     notepad `$PROFILE" -ForegroundColor Gray
Write-Host "     E adicione a linha acima" -ForegroundColor Gray


