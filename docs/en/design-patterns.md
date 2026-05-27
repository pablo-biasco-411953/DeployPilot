# Design Patterns

DeployPilot uses GoF patterns only where they reduce coupling or make a real extension point clearer. The goal is SOLID, readable code, and explicit seams for future features, not pattern-heavy architecture.

## Strategy

**Where:** `IRecipeSelectionStrategy`, `CustomCommandRecipeSelectionStrategy`, `TechnologyRecipeSelectionStrategy`.

**Why:** build recipe selection changes by repository context. A repository with an explicit custom command should use the custom command recipe, while most repositories should use the recipe attached to their detected technology.

**SOLID impact:**

- Single Responsibility: each strategy owns one selection rule.
- Open/Closed: new selection rules can be added without modifying the selector loop.
- Dependency Inversion: `RecipeSelector` depends on `IRecipeSelectionStrategy`, not on concrete rules.

## Factory Method

**Where:** `BuildRecipeSelectorFactory.CreateDefault()`.

**Why:** the default recipe selector needs an ordered strategy chain. Keeping that construction in one factory method avoids duplicating ordering decisions across API, server, tests or future tools.

**SOLID impact:**

- Single Responsibility: consumers ask for a selector instead of knowing how to assemble it.
- Open/Closed: the default strategy list can evolve in one place.

## Facade

**Where:** `RecipeSelector`, `DeployPilotApiClient`.

**Why:** both classes hide multiple smaller operations behind a simpler interface. `RecipeSelector` hides the strategy chain, while `DeployPilotApiClient` hides HTTP details from WPF clients and agents.

**SOLID impact:**

- Interface Segregation: callers use focused methods instead of raw infrastructure details.
- Dependency Inversion: UI code depends on a client abstraction over HTTP behavior.

## Repository

**Where:** `IDeployPilotStore`, `InMemoryDeployPilotStore`, `EfDeployPilotStore`.

**Why:** the app needs the same domain workflow over in-memory demos and real EF Core persistence. The store interface keeps API endpoints focused on orchestration instead of persistence details.

**Note:** Repository is not one of the original GoF patterns, but it is intentionally documented because it is central to persistence design.

## Patterns Avoided For Now

- Singleton: dependency injection already controls lifetimes and keeps tests clean.
- Abstract Factory: one simple factory method is enough for recipe selection today.
- Command: build jobs are domain records for now; a full command pipeline would be premature.
- Mediator/Event Bus: direct service calls are clearer until workflows become more distributed.
