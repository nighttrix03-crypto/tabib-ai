$ErrorActionPreference = 'Stop'
dotnet publish (Join-Path $PSScriptRoot 'TabibAI.csproj') `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output (Join-Path $PSScriptRoot 'dist') `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
Write-Host "Built: $PSScriptRoot\dist\TabibAI.exe"
