<#
.SYNOPSIS
    Build WebGL de entrega do Please Fix the City: Builds\WebGL (index.html na raiz) e Builds\itch\Please-Fix-the-City-WebGL.zip.

.DESCRIPTION
    Chama o Unity 6000.6.7f1 em batchmode com -executeMethod BuildScript.BuildWebGL (Assets\Editor\BuildScript.cs).
    O Unity Editor NÃO pode estar aberto no mesmo projeto: feche a janela do Editor antes (outro projeto aberto não atrapalha).
    A pasta Builds\WebGL é apagada e refeita a cada build.
    Por padrão o build liga o "Decompression Fallback" só durante o build (o valor do projeto volta depois): assim a
    página carrega no itch.io com ou sem o cabeçalho Content-Encoding e também num servidor local simples.
    -SemFallback usa o valor do projeto (hoje: Brotli sem fallback).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build\build_webgl.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\Build\build_webgl.ps1 -ProjectPath C:\Users\PIETRO\Projetos\Defesa-Civil-Game
#>
[CmdletBinding()]
param(
    # Pasta do projeto Unity. Padrão: a raiz do repositório onde este script está.
    [string]$ProjectPath,
    [string]$UnityExe,
    [switch]$Development,
    [switch]$SemFallback,
    [switch]$SemZip
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'BuildComum.ps1')
if (-not $UnityExe) { $UnityExe = $script:UnityPadrao }

$projeto = Resolve-Projeto $ProjectPath
Assert-Unity $UnityExe 'WebGLSupport'
if (Test-EditorAberto $projeto) {
    Write-Host "O Unity Editor está aberto neste projeto ($projeto). Feche o Editor e rode de novo." -ForegroundColor Red
    exit 2
}

$saida = Join-Path $projeto 'Builds\WebGL'
$carimbo = Get-Date -Format 'yyyyMMdd-HHmm'
$log = Join-Path $projeto "Builds\logs\webgl_$carimbo.log"
$hashPS = Get-HashArquivo (Join-Path $projeto 'ProjectSettings\ProjectSettings.asset')

Write-Host "Build WebGL de $projeto (costuma levar de 5 a 10 min)" -ForegroundColor Cyan
if (Test-Path -LiteralPath $saida) { Remove-Item -LiteralPath $saida -Recurse -Force }

$extras = @('-buildOutput', "`"$saida`"")
if ($Development) { $extras += '-development' }
if ($SemFallback) { $extras += '-webglNoFallback' }
$codigo = Invoke-UnityBuild -UnityExe $UnityExe -Projeto $projeto -BuildTarget 'WebGL' `
    -Metodo 'BuildScript.BuildWebGL' -Log $log -Extras $extras

$index = Join-Path $saida 'index.html'
$falhou = ($codigo -ne 0) -or -not (Test-Path -LiteralPath $index)
Show-ResumoLog $log $falhou
Test-ProjectSettings $projeto $hashPS
if ($falhou) {
    Write-Host 'Build WebGL FALHOU.' -ForegroundColor Red
    exit 1
}

# Limites do itch.io para jogo HTML5 (os da página de ajuda deles; confira na hora do envio).
$arquivos = @(Get-ChildItem -LiteralPath $saida -Recurse -File)
$total = ($arquivos | Measure-Object Length -Sum).Sum
$maior = $arquivos | Sort-Object Length -Descending | Select-Object -First 1
Write-Host "Build WebGL OK: $($arquivos.Count) arquivos, $(Format-MB $total); maior: $($maior.Name) ($(Format-MB $maior.Length))." -ForegroundColor Green
if ($arquivos.Count -gt 1000) { Write-Host 'AVISO: mais de 1000 arquivos (limite do itch.io para HTML5).' -ForegroundColor Yellow }
if ($maior.Length -gt 200MB) { Write-Host 'AVISO: arquivo maior que 200 MB (limite do itch.io por arquivo).' -ForegroundColor Yellow }
if ($total -gt 500MB) { Write-Host 'AVISO: mais de 500 MB descompactado (limite do itch.io para HTML5).' -ForegroundColor Yellow }

if (-not $SemZip) {
    $nome = 'Please-Fix-the-City-WebGL'
    if ($Development) { $nome += '_dev' }
    $zip = Join-Path $projeto "Builds\itch\$nome.zip"
    $n = New-ZipDaPasta -Origem $saida -Zip $zip
    Write-Host "Zip: $zip ($n arquivos, $(Format-MB (Get-Item -LiteralPath $zip).Length)); index.html na raiz do zip." -ForegroundColor Green
}
Write-Host "Teste local: python -m http.server 8000 --directory `"$saida`"  e abra http://localhost:8000"
exit 0
