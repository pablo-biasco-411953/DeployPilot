# Server Setup

The server is designed to start with a quick setup flow and an advanced flow.

## Quick Setup

1. Select InMemory, MySQL or Postgres.
2. Enter the connection string.
3. Choose an artifact root folder.
4. Create the admin user.
5. Register the first Git repository.
6. Select a build recipe template.

## Advanced Setup

Advanced setup exposes branch defaults, build variables, custom commands, artifact base URL, agent labels and language preferences.

## Persistence Providers

- `InMemory`: best for local demo and tests.
- `Postgres`: recommended for production and Supabase.
- `MySql`: useful for teams already running MySQL or MariaDB-compatible infrastructure.
