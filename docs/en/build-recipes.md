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

## Agent Git Preparation

Before a recipe runs, the agent prepares the workspace with Git:

- clone when the repository folder does not exist.
- fetch tags and branches for existing repositories.
- checkout the requested SHA when the build targets a historical version.
- fallback to the default branch when no SHA is requested.

`ExecuteGit` is disabled by default so local demos can run without network or credentials.

## Repository Probing

The API exposes `POST /api/repositories/probe` to inspect a local repository path from the server side. The probe detects supported project files and returns recipe suggestions so first setup can pre-fill repository technology and project path.

Detected files:

- `.sln`: MSBuild classic
- `.csproj`: C# WinForms
- `.vbproj`: VB.NET WinForms
- `.pjx` / `.prg`: FoxPro configurable command
