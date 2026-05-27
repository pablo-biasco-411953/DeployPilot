param(
  [Parameter(Mandatory = $true)][string]$RepositoryPath,
  [Parameter(Mandatory = $true)][string]$ProjectPath,
  [Parameter(Mandatory = $true)][string]$OutputPath,
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$project = Join-Path $RepositoryPath $ProjectPath
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
Write-Host "DeployPilot recipe: MSBuild classic"
msbuild $project /p:Configuration=$Configuration /p:OutDir="$OutputPath\"
