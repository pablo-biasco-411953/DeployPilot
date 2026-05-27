# Launcher Setup

The launcher needs the API endpoint, artifact endpoint and organization context. After setup, it can query available modules, detect updates, show changelogs, download artifacts and validate hashes.

## Demo Flow

1. Run `dotnet run --project DeployPilot.Api`.
2. Run `dotnet run --project DeployPilot.Launcher`.
3. Keep the default API endpoint or enter your own.
4. Click `Seed demo data`.
5. Click `Refresh`.

The launcher stores its local settings in `%LocalAppData%/DeployPilot/launcher-settings.json`.

## Update Safety

Before replacing files, the launcher should detect whether the target process is running. If it is open, the user must confirm before the process is closed and the update continues.
