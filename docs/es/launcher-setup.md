# Configuracion del launcher

El launcher necesita el endpoint de API, el endpoint de artefactos y la organizacion. Luego consulta modulos, detecta updates, muestra changelog, descarga artefactos y valida hashes.

## Flujo demo

1. Ejecutar `dotnet run --project DeployPilot.Api`.
2. Ejecutar `dotnet run --project DeployPilot.Launcher`.
3. Mantener el endpoint por defecto o ingresar otro.
4. Tocar `Seed demo data`.
5. Tocar `Refresh`.

El launcher guarda su configuracion local en `%LocalAppData%/DeployPilot/launcher-settings.json`.
