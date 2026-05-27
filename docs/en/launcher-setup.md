# Launcher Setup

The launcher needs the API endpoint, artifact endpoint and organization context. After setup, it can query available modules, detect updates, show changelogs, download artifacts and validate hashes.

## Update Safety

Before replacing files, the launcher should detect whether the target process is running. If it is open, the user must confirm before the process is closed and the update continues.
