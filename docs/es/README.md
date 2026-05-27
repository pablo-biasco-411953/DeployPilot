# DeployPilot

[English](../../README.md) | [Español](README.md)

DeployPilot es una plataforma para orquestar builds, versionar releases y distribuir aplicaciones de escritorio a multiples clientes o instalaciones sin copiar archivos a mano.

## Que resuelve

- Centraliza repositorios, aplicaciones y modulos.
- Ejecuta builds mediante recetas reutilizables.
- Publica artefactos con hash e historial.
- Permite que un launcher detecte updates y rollback.
- Separa organizaciones/clientes para evitar mezclas de configuracion.

## Inicio rapido

```powershell
dotnet restore
dotnet test
dotnet run --project DeployPilot.Api
dotnet run --project DeployPilot.Artifacts
```

## Documentacion

- [Arquitectura](architecture.md)
- [Configuracion del server](server-setup.md)
- [Configuracion del launcher](launcher-setup.md)
- [Recetas de build](build-recipes.md)
