# Configuração do Node.js e npm

Este projeto usa `semantic-release` que requer Node.js e npm instalados localmente.

## Instalação no Windows

### Opção 1: Instalação direta (mais simples)

1. Acesse [https://nodejs.org](https://nodejs.org)
2. Baixe a versão **LTS** (recomendada)
3. Execute o instalador `.msi`
4. Siga as instruções, mantendo as opções padrão
5. Reinicie o terminal/PowerShell após a instalação

### Opção 2: Usando nvm-windows (recomendado para desenvolvedores)

1. Baixe o nvm-windows de: [https://github.com/coreybutler/nvm-windows/releases](https://github.com/coreybutler/nvm-windows/releases)
2. Instale o arquivo `nvm-setup.exe`
3. Abra um novo terminal e execute:
   ```powershell
   nvm install 20.11.0
   nvm use 20.11.0
   ```

## Verificação da Instalação

Após instalar, execute no terminal:

```powershell
node --version
npm --version
```

Você deve ver as versões instaladas. Para este projeto, recomendamos:
- Node.js: versão 18 ou superior (20 LTS recomendado)
- npm: vem junto com Node.js

## Instalação das Dependências do Projeto

Após instalar Node.js, execute no diretório do projeto:

```powershell
npm install
```

Isso instalará todas as dependências do `semantic-release` listadas no `package.json`.

## Troubleshooting

### Comando não encontrado após instalação

Se após instalar o Node.js o comando não for reconhecido no PowerShell:

1. **Solução Rápida**: Feche e reabra o terminal/PowerShell
2. **Solução Permanente**: Adicione ao perfil do PowerShell:
   ```powershell
   # Recarregar PATH do sistema no inicio da sessao
   $env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')
   ```
   
   Para adicionar automaticamente:
   ```powershell
   if (-not (Test-Path $PROFILE)) { New-Item -Path $PROFILE -ItemType File -Force }
   Add-Content -Path $PROFILE -Value "`$env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')"
   ```
   
   Depois, recarregue o perfil:
   ```powershell
   . $PROFILE
   ```

3. **Verificar PATH**: Execute:
   ```powershell
   $env:PATH -split ';' | Select-String node
   ```
   
   Se não aparecer nada, o Node.js pode não estar no PATH do sistema.

### Script de Diagnóstico

Execute o script de diagnóstico para verificar o PATH:
```powershell
.\scripts\fix-powershell-path.ps1
```

