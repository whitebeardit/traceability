# Node.js and npm Setup

This project uses `semantic-release` which requires Node.js and npm installed locally.

## Installation on Windows

### Option 1: Direct installation (simplest)

1. Go to [nodejs.org](https://nodejs.org)
2. Download the **LTS** version (recommended)
3. Run the `.msi` installer
4. Follow the instructions, keeping default options
5. Restart terminal/PowerShell after installation

### Option 2: Using nvm-windows (recommended for developers)

1. Download nvm-windows from: [nvm-windows releases](https://github.com/coreybutler/nvm-windows/releases)
2. Install the `nvm-setup.exe` file
3. Open a new terminal and run:
   ```powershell
   nvm install 20.11.0
   nvm use 20.11.0
   ```

## Installation Verification

After installing, run in terminal:

```powershell
node --version
npm --version
```

You should see the installed versions. For this project, we recommend:
- Node.js: version 18 or higher (20 LTS recommended)
- npm: comes with Node.js

## Installing Project Dependencies

After installing Node.js, run in the project directory:

```powershell
npm install
```

This will install all `semantic-release` dependencies listed in `package.json`.

## Troubleshooting

### Command not found after installation

If after installing Node.js the command is not recognized in PowerShell:

1. **Quick Solution**: Close and reopen terminal/PowerShell
2. **Permanent Solution**: Add to PowerShell profile:
   ```powershell
   # Reload system PATH at session start
   $env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')
   ```
   
   To add automatically:
   ```powershell
   if (-not (Test-Path $PROFILE)) { New-Item -Path $PROFILE -ItemType File -Force }
   Add-Content -Path $PROFILE -Value "`$env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')"
   ```
   
   Then, reload the profile:
   ```powershell
   . $PROFILE
   ```

3. **Check PATH**: Run:
   ```powershell
   $env:PATH -split ';' | Select-String node
   ```
   
   If nothing appears, Node.js may not be in the system PATH.

### Diagnostic Script

Run the diagnostic script to check PATH:
```powershell
.\scripts\fix-powershell-path.ps1
```
