[CmdletBinding()]
param(
    [ValidateSet('Setup', 'Doctor', 'Build', 'Deploy', 'Verify')]
    [string]$Action = 'Doctor',
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$localRoot = Join-Path $repoRoot '.local'
$configPath = Join-Path $localRoot 'environment.json'
$archivePath = Join-Path $localRoot 'BepInEx-791.zip'
$dependencyRoot = Join-Path $localRoot 'BepInEx-791'
$archiveHash = 'BB1C510909B69522733791F3A0817A487379A52DA6468A492F1C9C19B3EC6F9A'
$archiveUrl = 'https://builds.bepinex.dev/projects/bepinex_be/791/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.791%2Bb782da4.zip'
$projectPath = Join-Path $repoRoot 'src/Dwno.Archipelago/Dwno.Archipelago.csproj'
$builtDll = Join-Path $repoRoot 'src/Dwno.Archipelago/bin/Release/net6.0/Dwno.Archipelago.dll'

if (-not $GamePath -and (Test-Path -LiteralPath $configPath)) {
    $GamePath = (Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json).GamePath
}

function Assert-GamePath {
    if (-not $GamePath -or -not (Test-Path -LiteralPath (Join-Path $GamePath 'Digimon World Next Order.exe'))) {
        throw 'Indiquer le dossier du jeu : ./scripts/dev.ps1 Setup -GamePath "C:/chemin/du/jeu"'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $GamePath 'GameAssembly.dll'))) {
        throw 'GameAssembly.dll absent : installation IL2CPP attendue.'
    }
}

function Build-Plugin {
    Push-Location $repoRoot
    try {
        & dotnet build $projectPath -c Release
        if ($LASTEXITCODE -ne 0) { throw 'La compilation a échoué.' }
    } finally { Pop-Location }
}

switch ($Action) {
    'Setup' {
        Assert-GamePath
        New-Item -ItemType Directory -Path $localRoot -Force | Out-Null
        if (-not (Test-Path -LiteralPath $archivePath)) {
            Invoke-WebRequest -Uri $archiveUrl -OutFile $archivePath
        }
        if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $archiveHash) {
            throw 'Empreinte BepInEx différente de la version verrouillée. Archive non extraite.'
        }
        # Extraire uniquement les références locales ; ne jamais écraser le chargeur du jeu.
        Expand-Archive -LiteralPath $archivePath -DestinationPath $dependencyRoot -Force
        @{ GamePath = (Resolve-Path -LiteralPath $GamePath).Path } |
            ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding UTF8
        Write-Output 'Dépendances préparées et chemin local enregistré. Configuration exclue de Git.'
    }
    'Doctor' {
        Push-Location $repoRoot
        try {
            & dotnet --version
            if ($LASTEXITCODE -ne 0) { throw 'SDK .NET 6.0.428 absent.' }
        } finally { Pop-Location }
        if (-not (Test-Path -LiteralPath (Join-Path $dependencyRoot 'BepInEx/core/BepInEx.Unity.IL2CPP.dll'))) {
            throw 'Références absentes : exécuter Setup.'
        }
        Assert-GamePath
        foreach ($relativePath in @('winhttp.dll', 'doorstop_config.ini', 'BepInEx/core/BepInEx.Unity.IL2CPP.dll')) {
            if (-not (Test-Path -LiteralPath (Join-Path $GamePath $relativePath))) {
                throw "BepInEx incomplet : $relativePath absent. Voir le guide DWNO-001."
            }
        }
        Write-Output 'SDK, références de compilation et installation BepInEx présents.'
    }
    'Build' { Build-Plugin }
    'Deploy' {
        Assert-GamePath
        if (Get-Process -Name 'Digimon World Next Order' -ErrorAction SilentlyContinue) {
            throw 'Fermer le jeu avant de déployer ; aucune DLL remplacée.'
        }
        if (-not (Test-Path -LiteralPath (Join-Path $GamePath 'BepInEx/core/BepInEx.Unity.IL2CPP.dll'))) {
            throw 'Installer BepInEx IL2CPP avant le déploiement.'
        }
        Build-Plugin
        $pluginDir = Join-Path $GamePath 'BepInEx/plugins/Dwno.Archipelago'
        New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
        $destination = Join-Path $pluginDir 'Dwno.Archipelago.dll'
        if (Test-Path -LiteralPath $destination) {
            $backupDir = Join-Path $localRoot 'backups'
            New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
            Copy-Item -LiteralPath $destination -Destination (Join-Path $backupDir ("Dwno.Archipelago-{0}.dll" -f (Get-Date -Format 'yyyyMMdd-HHmmss-fff')))
        }
        Copy-Item -LiteralPath $builtDll -Destination $destination
        if ((Get-FileHash $builtDll).Hash -ne (Get-FileHash $destination).Hash) { throw 'Copie de DLL non conforme.' }
        Write-Output 'DLL déployée. Lancer le jeu depuis Steam puis exécuter Verify.'
    }
    'Verify' {
        Assert-GamePath
        $installedDll = Join-Path $GamePath 'BepInEx/plugins/Dwno.Archipelago/Dwno.Archipelago.dll'
        if (-not (Test-Path -LiteralPath $builtDll) -or -not (Test-Path -LiteralPath $installedDll)) {
            throw 'DLL compilée ou déployée absente.'
        }
        if ((Get-FileHash $builtDll).Hash -ne (Get-FileHash $installedDll).Hash) { throw 'DLL compilée et déployée différentes.' }
        $logPath = Join-Path $GamePath 'BepInEx/LogOutput.log'
        if (-not (Test-Path -LiteralPath $logPath)) { throw 'Log absent : lancer le jeu.' }
        $installedTime = (Get-Item -LiteralPath $installedDll).LastWriteTimeUtc
        if ((Get-Item -LiteralPath $logPath).LastWriteTimeUtc -lt $installedTime) {
            throw 'Log antérieur à la DLL : relancer le jeu avant de valider.'
        }
        $log = Get-Content -LiteralPath $logPath -Raw
        if ($log -notmatch '\[DWNO-001\] DWNO Archipelago 0\.1\.0 loaded successfully\.' -or
            $log -notmatch 'Chainloader startup complete') { throw 'Chargement complet non confirmé dans le log.' }
        Write-Output 'DWNO-001 : DLL identique, message de chargement présent, chainloader démarré.'
        Write-Output 'Ce contrôle ne valide ni le menu visuel ni les futurs hooks de gameplay.'
    }
}
