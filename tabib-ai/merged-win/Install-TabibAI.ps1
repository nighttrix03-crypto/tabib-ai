#Requires -RunAsAdministrator
<#
  TabibAI - تثبيت + اختصار تلقائي
  ينسخ TabibAI.exe إلى Program Files وينشئ اختصار سطح مكتب + قائمة ابدأ بالأيقونة المميزة
#>
$ErrorActionPreference = 'Stop'
$InstallDir = "$env:ProgramFiles\TabibAI"
$Desktop = [Environment]::GetFolderPath('CommonDesktopDirectory')
$StartMenu = Join-Path ([Environment]::GetFolderPath('CommonStartMenu')) 'Programs\TabibAI'

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path $StartMenu | Out-Null

Copy-Item -Force (Join-Path $PSScriptRoot 'TabibAI.exe') (Join-Path $InstallDir 'TabibAI.exe')
Copy-Item -Force (Join-Path $PSScriptRoot 'tabib-ai.ico') (Join-Path $InstallDir 'tabib-ai.ico')

$Wsh = New-Object -ComObject WScript.Shell
foreach ($link in @((Join-Path $Desktop 'Tabib AI.lnk'), (Join-Path $StartMenu 'Tabib AI.lnk'))) {
  $sc = $Wsh.CreateShortcut($link)
  $sc.TargetPath = (Join-Path $InstallDir 'TabibAI.exe')
  $sc.WorkingDirectory = $InstallDir
  $sc.IconLocation = (Join-Path $InstallDir 'tabib-ai.ico')
  $sc.Description = 'طبيب AI - مساعد طبي ذكي محلي'
  $sc.Save()
}
Write-Host "OK: Tabib AI installed + shortcuts created" -ForegroundColor Green
