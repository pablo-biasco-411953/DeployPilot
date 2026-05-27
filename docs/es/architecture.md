# Arquitectura

DeployPilot separa la coordinacion de la compilacion. La API, la orquestacion y la entrega de artefactos pueden correr como servicios, mientras que las builds de escritorio Windows quedan a cargo de un agente Windows nativo.

## Componentes

- Server WPF: consola local, setup, diagnostico, tray y metricas.
- Launcher WPF: instalador y actualizador para el cliente.
- API: entrada HTTP multi-organizacion.
- Persistence: almacenamiento EF Core para InMemory, Postgres/Supabase y MySQL.
- Orchestrator: cola, locks, cancelacion y estados.
- Artifacts: servidor HTTP liviano para builds publicadas.
- Agent: worker Windows que ejecuta recetas.
