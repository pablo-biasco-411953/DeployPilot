param(
  [Parameter(Mandatory = $true)][string]$RepositoryPath,
  [Parameter(Mandatory = $true)][string]$ProjectPath,
  [Parameter(Mandatory = $true)][string]$OutputPath,
  [Parameter(Mandatory = $true)][string]$BuildCommand
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
Push-Location $RepositoryPath
try {
  Write-Host "DeployPilot recipe: FoxPro configurable command"
  Invoke-Expression $BuildCommand
}
finally {
  Pop-Location
}
