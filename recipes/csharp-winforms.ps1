param(
  [Parameter(Mandatory = $true)][string]$RepositoryPath,
  [Parameter(Mandatory = $true)][string]$ProjectPath,
  [Parameter(Mandatory = $true)][string]$OutputPath,
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
& "$PSScriptRoot\msbuild-classic.ps1" -RepositoryPath $RepositoryPath -ProjectPath $ProjectPath -OutputPath $OutputPath -Configuration $Configuration
