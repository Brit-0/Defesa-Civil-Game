# Funções comuns de build_windows.ps1 e build_webgl.ps1 (carregado com ". BuildComum.ps1").
# Compatível com Windows PowerShell 5.1 e PowerShell 7.

$script:UnityPadrao = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe'

function Resolve-Projeto {
    param([string]$ProjectPath)
    if (-not $ProjectPath) { $ProjectPath = Join-Path $PSScriptRoot '..\..' }
    $abs = (Resolve-Path -LiteralPath $ProjectPath).Path.TrimEnd('\')
    if (-not (Test-Path (Join-Path $abs 'Assets\Editor\BuildScript.cs'))) {
        throw "Falta Assets\Editor\BuildScript.cs em $abs. Faça o merge da branch jam-itch nesta pasta antes do build."
    }
    if (-not (Test-Path (Join-Path $abs 'Assets\Scenes\Gameplay.unity'))) {
        throw "Não achei Assets\Scenes\Gameplay.unity em $abs. Isso é a pasta do projeto Unity?"
    }
    return $abs
}

function Assert-Unity {
    param([string]$UnityExe, [string]$Modulo)
    if (-not (Test-Path -LiteralPath $UnityExe)) {
        throw "Não achei o Unity em $UnityExe. Instale o 6000.6.7f1 pelo Unity Hub ou passe -UnityExe."
    }
    $engines = Join-Path (Split-Path $UnityExe) 'Data\PlaybackEngines'
    if (-not (Test-Path (Join-Path $engines $Modulo))) {
        throw "O módulo $Modulo não está instalado neste Unity (Unity Hub > Installs > 6000.6.7f1 > Add modules)."
    }
}

# O Editor aberto no mesmo projeto segura Temp\UnityLockfile; o batchmode não abre e o build falha.
function Test-EditorAberto {
    param([string]$Projeto)
    $lock = Join-Path $Projeto 'Temp\UnityLockfile'
    if (Test-Path -LiteralPath $lock) {
        try {
            $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
            $fs.Close()
        } catch {
            return $true
        }
    }
    $padrao = [regex]::Escape($Projeto) + '(?=["\\/\s]|$)'
    $procs = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue
    foreach ($p in $procs) {
        if ($p.CommandLine -and [regex]::IsMatch($p.CommandLine, $padrao, 'IgnoreCase')) { return $true }
    }
    return $false
}

function Get-HashArquivo {
    param([string]$Caminho)
    if (-not (Test-Path -LiteralPath $Caminho)) { return '' }
    return (Get-FileHash -LiteralPath $Caminho -Algorithm SHA256).Hash
}

function Get-UltimaLinha {
    param([string]$Log)
    if (-not (Test-Path -LiteralPath $Log)) { return '' }
    try {
        $fs = [System.IO.File]::Open($Log, 'Open', 'Read', 'ReadWrite')
        try {
            $tam = [Math]::Min($fs.Length, 4096)
            [void]$fs.Seek(-$tam, 'End')
            $buf = New-Object byte[] $tam
            [void]$fs.Read($buf, 0, $tam)
        } finally { $fs.Close() }
        $linhas = [System.Text.Encoding]::UTF8.GetString($buf) -split "`r?`n" | Where-Object { $_.Trim() }
        if (-not $linhas) { return '' }
        $ultima = @($linhas)[-1].Trim()
        if ($ultima.Length -gt 110) { $ultima = $ultima.Substring(0, 110) + '...' }
        return $ultima
    } catch {
        return ''
    }
}

# Roda o Unity em batchmode e espera, mostrando a última linha do log a cada 30 s. Devolve o código de saída.
function Invoke-UnityBuild {
    param(
        [string]$UnityExe,
        [string]$Projeto,
        [string]$BuildTarget,
        [string]$Metodo,
        [string]$Log,
        [string[]]$Extras = @()
    )
    New-Item -ItemType Directory -Force (Split-Path $Log) | Out-Null
    if (-not (Test-Path (Join-Path $Projeto 'Library'))) {
        Write-Host 'Sem a pasta Library: o Unity vai importar o projeto inteiro antes do build (pode levar de 10 a 40 min).' -ForegroundColor Yellow
    }
    $argumentos = @(
        '-batchmode', '-quit',
        '-projectPath', "`"$Projeto`"",
        '-buildTarget', $BuildTarget,
        '-executeMethod', $Metodo,
        '-logFile', "`"$Log`""
    ) + $Extras
    $linha = $argumentos -join ' '
    Write-Host "Unity: $linha" -ForegroundColor DarkGray
    $inicio = Get-Date
    $proc = Start-Process -FilePath $UnityExe -ArgumentList $linha -PassThru -WindowStyle Hidden
    $null = $proc.Handle
    while (-not $proc.WaitForExit(30000)) {
        $min = [int]((Get-Date) - $inicio).TotalMinutes
        Write-Host ("  [{0} min] {1}" -f $min, (Get-UltimaLinha $Log))
    }
    $proc.WaitForExit()
    $seg = [int]((Get-Date) - $inicio).TotalSeconds
    Write-Host "Unity terminou em $seg s com código $($proc.ExitCode)."
    return $proc.ExitCode
}

function Show-ResumoLog {
    param([string]$Log, [bool]$Falhou)
    if (-not (Test-Path -LiteralPath $Log)) {
        Write-Host "Log não encontrado: $Log" -ForegroundColor Red
        return
    }
    $padrao = '\[BuildScript\]|error CS\d+|Scripts have compiler errors|Aborting batchmode|executeMethod class|No valid Unity Editor license|Build Finished, Result'
    $achados = Select-String -LiteralPath $Log -Pattern $padrao | Select-Object -First 30
    foreach ($a in $achados) { Write-Host "  $($a.Line.Trim())" }
    if ($Falhou) {
        Write-Host '--- últimas 20 linhas do log ---' -ForegroundColor Yellow
        Get-Content -LiteralPath $Log -Tail 20 | ForEach-Object { Write-Host "  $_" }
    }
    Write-Host "Log completo: $Log"
}

# Zipa o conteúdo da pasta (não a pasta), com '/' nos caminhos internos: o itch.io e o unzip do Linux
# não entendem a barra invertida que o Compress-Archive do PowerShell 5.1 grava.
function New-ZipDaPasta {
    param([string]$Origem, [string]$Zip, [string[]]$Excluir = @())
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    New-Item -ItemType Directory -Force (Split-Path $Zip) | Out-Null
    if (Test-Path -LiteralPath $Zip) { Remove-Item -LiteralPath $Zip -Force }
    $base = (Resolve-Path -LiteralPath $Origem).Path.TrimEnd('\') + '\'
    $arquivo = [System.IO.Compression.ZipFile]::Open($Zip, 'Create')
    $n = 0
    try {
        foreach ($f in Get-ChildItem -LiteralPath $Origem -Recurse -File) {
            $rel = $f.FullName.Substring($base.Length).Replace('\', '/')
            $pular = $false
            foreach ($p in $Excluir) { if ($rel -like $p) { $pular = $true; break } }
            if ($pular) { continue }
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($arquivo, $f.FullName, $rel, 'Optimal')
            $n++
        }
    } finally {
        $arquivo.Dispose()
    }
    return $n
}

function Format-MB {
    param([double]$Bytes)
    return ('{0:0.0} MB' -f ($Bytes / 1MB))
}

function Test-ProjectSettings {
    param([string]$Projeto, [string]$HashAntes)
    $ps = Join-Path $Projeto 'ProjectSettings\ProjectSettings.asset'
    if ((Get-HashArquivo $ps) -ne $HashAntes) {
        Write-Host 'AVISO: ProjectSettings.asset mudou durante o build. Confira com "git diff ProjectSettings" antes de commitar.' -ForegroundColor Yellow
    }
}
