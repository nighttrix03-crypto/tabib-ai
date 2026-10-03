$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& (Join-Path $root 'build-windows.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Building TabibAI.exe failed.' }
dotnet publish (Join-Path $root 'installer/TabibAIInstaller.csproj') `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output (Join-Path $root 'installer/dist') `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
if ($LASTEXITCODE -ne 0) { throw 'Building the setup program failed.' }
Write-Host "Built: $(Join-Path $root 'installer/dist/TabibAI-Setup.exe')"
