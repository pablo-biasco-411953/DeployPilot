# Patrones de Diseno

DeployPilot usa patrones GoF solo cuando bajan acoplamiento o dejan mas claro un punto real de extension. La meta es SOLID, codigo legible y puntos de crecimiento explicitos, no una arquitectura cargada de patrones.

## Strategy

**Donde:** `IRecipeSelectionStrategy`, `CustomCommandRecipeSelectionStrategy`, `TechnologyRecipeSelectionStrategy`.

**Por que:** la seleccion de recetas cambia segun el contexto del repositorio. Si un repositorio define un comando custom, debe usar la receta custom. Si no, usa la receta asociada a la tecnologia detectada.

**Impacto SOLID:**

- Single Responsibility: cada estrategia maneja una sola regla.
- Open/Closed: se pueden agregar reglas sin modificar el loop del selector.
- Dependency Inversion: `RecipeSelector` depende de `IRecipeSelectionStrategy`, no de reglas concretas.

## Factory Method

**Donde:** `BuildRecipeSelectorFactory.CreateDefault()`.

**Por que:** el selector default necesita una cadena ordenada de estrategias. Centralizar esa construccion evita duplicar decisiones de orden en API, server, tests o herramientas futuras.

**Impacto SOLID:**

- Single Responsibility: los consumidores piden un selector sin saber como se arma.
- Open/Closed: la lista default puede evolucionar en un solo lugar.

## Facade

**Donde:** `RecipeSelector`, `DeployPilotApiClient`.

**Por que:** ambos esconden varias operaciones chicas detras de una interfaz mas simple. `RecipeSelector` esconde la cadena de estrategias y `DeployPilotApiClient` esconde detalles HTTP para WPF, agentes y futuras herramientas.

**Impacto SOLID:**

- Interface Segregation: los consumidores usan metodos enfocados en vez de detalles de infraestructura.
- Dependency Inversion: la UI depende de un cliente por encima del comportamiento HTTP.

## Repository

**Donde:** `IDeployPilotStore`, `InMemoryDeployPilotStore`, `EfDeployPilotStore`.

**Por que:** la app necesita el mismo flujo de dominio sobre demos en memoria y persistencia real con EF Core. La interfaz del store mantiene los endpoints enfocados en orquestacion y no en detalles de persistencia.

**Nota:** Repository no pertenece a la lista GoF original, pero queda documentado porque es central en el diseno de persistencia.

## Patrones Evitados Por Ahora

- Singleton: dependency injection ya controla lifetimes y mantiene los tests limpios.
- Abstract Factory: hoy alcanza con un factory method simple para recetas.
- Command: los build jobs son records de dominio por ahora; un pipeline completo seria prematuro.
- Mediator/Event Bus: las llamadas directas son mas claras hasta que los workflows se vuelvan mas distribuidos.
