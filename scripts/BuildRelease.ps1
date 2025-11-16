# Copyright (c) 2024.
# This file is part of UniversalExtractor and is licensed under the GNU General Public License v3.0.
# See the LICENSE file distributed with this work for additional information.

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$PackageMsix,
    [string]$MakeAppxPath = "makeappx.exe"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "UniversalExtractor.App\UniversalExtractor.App.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\$Runtime\$Configuration"

Write-Host "Publishing $projectPath ($Configuration | $Runtime)..."
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained:$false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $publishDir | Out-Host

if (-not $PackageMsix) {
    Write-Host "Publish artifacts available in $publishDir"
    return
}

if (-not (Test-Path $MakeAppxPath)) {
    throw "makeappx.exe not found. Provide the full path via -MakeAppxPath."
}

$layoutRoot = Join-Path $repoRoot "artifacts\msix-layout"
if (Test-Path $layoutRoot) {
    Remove-Item $layoutRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $layoutRoot | Out-Null

Copy-Item (Join-Path $repoRoot "Packaging\AppxManifest.xml") (Join-Path $layoutRoot "AppxManifest.xml")
Copy-Item (Join-Path $repoRoot "Packaging\Assets") -Destination (Join-Path $layoutRoot "Assets") -Recurse
Copy-Item (Join-Path $publishDir "*") -Destination $layoutRoot -Recurse

$msixOutput = Join-Path $repoRoot "artifacts\UniversalExtractor.msix"
if (Test-Path $msixOutput) {
    Remove-Item $msixOutput -Force
}

& $MakeAppxPath pack /d $layoutRoot /p $msixOutput | Out-Host
Write-Host "MSIX package created at $msixOutput (remember to sign before distribution)."
