# Architecture

DeployPilot separates coordination from compilation. API, orchestration and artifact delivery can run as services, while Windows desktop compilation is delegated to a native Windows agent.

## Components

- Server WPF: local admin console, setup, diagnostics, tray mode and metrics.
- Launcher WPF: customer-facing updater and installer.
- API: tenant-aware HTTP gateway.
- Persistence: EF Core storage for InMemory, Postgres/Supabase and MySQL.
- Orchestrator: queue, locks, cancellation and status transitions.
- Artifacts: lightweight HTTP server for published builds.
- Agent: Windows worker that runs repository-specific recipes.

## Data Flow

1. An organization registers repositories, applications and modules.
2. A build request is queued for a module.
3. The API persists the request through the configured EF Core provider.
4. The orchestrator leases the job only when its lock is free.
5. The Windows agent checks out the requested Git SHA and executes the selected recipe.
6. The artifact service publishes a zip and manifest.
7. Launchers query the API and download validated artifacts when an update exists.
