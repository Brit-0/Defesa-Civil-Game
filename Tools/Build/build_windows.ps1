<#
.SYNOPSIS
    Build Windows de entrega do Please Fix the City: Builds\Windows\PleaseFixTheCity.exe e Builds\itch\Please-Fix-the-City-Windows.zip.

.DESCRIPTION
    Chama o Unity 6000.6.7f1 em batchmode com -executeMethod BuildScript.BuildWindows (Assets\Editor\BuildScript.cs).
    O Unity Editor NÃO pode estar aberto no mesmo projeto: feche a janela do Editor antes (outro projeto aberto não atrapalha).
    A pasta Builds\Windows é apagada e refeita a cada build.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build\build_windows.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build\build_windows.ps1 -ProjectPath C:\Users\PIETRO\Projetos\Defesa-Civil-Game -Development
#>
[CmdletBinding()]
param(
    # Pasta do projeto Unity. Padrão: a raiz do repositório onde este script está.
    [string]$ProjectPath,
    [string]$UnityExe,
    # Build de desenvolvimento (console de erros na tela). O zip ganha o sufixo _dev.
    [switch]$Development,
    [switch]$SemZip
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'BuildComum.ps1')
if (-not $UnityExe) { $UnityExe = $script:UnityPadrao }

$projeto = Resolve-Projeto $ProjectPath
Assert-Unity $UnityExe 'windowsstandalonesupport'
if (Test-EditorAberto $projeto) {
    Write-Host "O Unity Editor está aberto neste projeto ($projeto). Feche o Editor e rode de novo." -ForegroundColor Red
    exit 2
}

$saida = Join-Path $projeto 'Builds\Windows'
$exe = Join-Path $saida 'PleaseFixTheCity.exe'
$carimbo = Get-Date -Format 'yyyyMMdd-HHmm'
$log = Join-Path $projeto "Builds\logs\windows_$carimbo.log"
$hashPS = Get-HashArquivo (Join-Path $projeto 'ProjectSettings\ProjectSettings.asset')

Write-Host "Build Windows de $projeto" -ForegroundColor Cyan
if (Test-Path -LiteralPath $saida) { Remove-Item -LiteralPath $saida -Recurse -Force }

$extras = @('-buildOutput', "`"$exe`"")
if ($Development) { $extras += '-development' }
$codigo = Invoke-UnityBuild -UnityExe $UnityExe -Projeto $projeto -BuildTarget 'Win64' `
    -Metodo 'BuildScript.BuildWindows' -Log $log -Extras $extras

$falhou = ($codigo -ne 0) -or -not (Test-Path -LiteralPath $exe)
Show-ResumoLog $log $falhou
Test-ProjectSettings $projeto $hashPS
if ($falhou) {
    Write-Host 'Build Windows FALHOU.' -ForegroundColor Red
    exit 1
}

$tamanho = (Get-ChildItem -LiteralPath $saida -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host "Build Windows OK: $exe ($(Format-MB $tamanho) na pasta)." -ForegroundColor Green

if (-not $SemZip) {
    $nome = 'Please-Fix-the-City-Windows'
    if ($Development) { $nome += '_dev' }
    $zip = Join-Path $projeto "Builds\itch\$nome.zip"
    $fora = @('*_BurstDebugInformation_DoNotShip/*', '*_BackUpThisFolder_ButDontShipItWithYourGame/*')
    $n = New-ZipDaPasta -Origem $saida -Zip $zip -Excluir $fora
    Write-Host "Zip: $zip ($n arquivos, $(Format-MB (Get-Item -LiteralPath $zip).Length))." -ForegroundColor Green
}
exit 0
