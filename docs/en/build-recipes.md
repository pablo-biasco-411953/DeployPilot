# Build Recipes

Recipes are PowerShell adapters selected by repository technology. DeployPilot keeps orchestration in .NET and leaves technology-specific build details in small scripts.

## Initial Recipes

- `msbuild-classic.ps1`
- `dotnet-sdk.ps1`
- `csharp-winforms.ps1`
- `vbnet-winforms.ps1`
- `foxpro.ps1`
- `custom-command.ps1`

Each recipe receives repository path, project path, output path and an optional custom build command.
