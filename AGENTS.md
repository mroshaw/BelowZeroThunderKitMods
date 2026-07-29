# AGENTS.md — Subnautica: Below Zero Mods

Guidance for any AI agent (or human) contributing code to this repository. This file defines the standards, conventions, and architectural expectations for the project. Treat it as binding unless the user explicitly overrides a rule for a specific task.

## Project Overview

The project contains multiple "mods" for the Subnautica: Below Zero game. The project uses Unity 2019.4.36f1, the same version used to build the latest version of the game. The project uses the "ThunderKit" Unity game modding framework. Mods are typically a combination of C# scripts that implement custom components, custom assets in Asset Bundles, and game method patches using the "HarmonyX" C# modding library. Most mods make use of the "Nautilus" Subnautica: Below Zero modding library.

## Tooling

- Unity: 2019.3.36f1
- JetBrains Rider: 2026.2
- Odin Inspector: 4.0.2
- Nautilus BZ: 1.0.0-pre 5.2
- Subnautica: Below Zero game: Oct-2025 53872
- ThunderKit: 9.4.2

## Core Principles

1. **Component-based design.** Favour small, single-purpose `MonoBehaviour`s and plain C# classes composed together over large monolithic scripts. Each component should have one clear reason to change.
2. **Separation of concerns.** Presentation, input, game logic, and data should be decoupled. Prefer interfaces and events/delegates over direct cross-referencing between unrelated systems.
3. **Performance-first, GC-aware.** Minimise per-frame allocations. Avoid LINQ and closures in hot paths (`Update`, `FixedUpdate`, physics callbacks). Cache component references, reuse collections, prefer `struct` for small value types, and pool objects (bricks, balls, particles, power-ups) rather than instantiating/destroying at runtime.
4. **Elegant, not clever.** Prefer clear, idiomatic solutions over dense or overly abstract ones. If a simpler approach achieves the same result with equal performance, use it.
5. **Documentation with restraint.** Use XML doc comments (`///`) on public APIs, especially non-obvious ones. Avoid comments that merely restate the code. Comment *why*, not *what*, when the *why* isn't obvious.

## C# Standards

- Target C# 7.3 language features only, as this is the version officially supported in Unity 2019.4.
- Follow Microsoft's [identifier naming conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) and [coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):
  - `PascalCase` for types, methods, properties, public fields, events, namespaces.
  - `camelCase` for local variables and parameters.
  - Private/serialized backing fields: `camelCase`, no underscore prefix unless the team later agrees otherwise — currently follow MS convention (no `_` prefix) for consistency with the linked style guide.
  - `PascalCase` for constants.
  - Interfaces prefixed with `I` (e.g. `IDamageable`).
  - One class per file, filename matches class name.
- Always specify variable type, do not use `var`.
- Prefer expression-bodied members for trivial one-liners; use full bodies where logic is non-trivial.
- Use `readonly` for fields that are never reassigned after construction/initialization.
- Avoid `public` fields — use `[SerializeField] private` (or Odin-attributed equivalents) with properties where external access is required.
- Null-check with pattern matching (`if (obj is null)`) rather than `== null` where practical, respecting Unity's overridden `==` for `UnityEngine.Object` (never use `is null`/`??`/`?.` on `UnityEngine.Object`-derived types - use Unity's own null check).

## Unity-Specific Conventions

- **Lifecycle methods:** keep `Awake`, `OnEnable`, `Start`, `Update`, `OnDisable`, `OnDestroy` lean. Delegate real work to dedicated methods.
- **Caching:** cache `Transform`, `Camera`, and other frequently accessed components in `Awake`/`Start` rather than repeated `GetComponent` calls.
- **Events:** use C# events/`UnityEvent` (sparingly, prefer C# events for perf-sensitive internal code, `UnityEvent` only where inspector wiring adds real value) to decouple systems rather than direct references between managers and gameplay objects.
- **Physics:** use `FixedUpdate` for physics-driven movement.
- **Folder structure**: follow the official Unity guidelines in their "[Organising your project](https://unity.com/how-to/organizing-your-project)" pages.
- **Editor inspector**: use Odin Inspector attributes for editor inspector facing serialised class properties.

## Project Structure

The project structure is as below. Each "mod" has it's own subfolder in "Mods". The "GameFiles~" folder, invisible to the Unity editor due to the tilda extension, is a full export of the Subnautica game files, created using "Asset Ripper". Files within this folder should be considered "read only", and are for reference only, or for use in generating code and assets that reference "vanilla" game entities:

```
Assets/
  Mods/            			# Parent folder containining individual mod folders
  	DaftAppleModTools_BZ/	# A special mod that contains shared functionality shipped with each mod
  	ExampleMod/				# Each mod has it's own subfolder
  		Prefabs/			# Follow Unity's folder structure guidelines, depending on the content of each mod
  		Scenes/
  		Scripts/			# All scripts go in here, with Core and Editor having their own asmdefs
  			Core/			# Game scripts go here
  			Editor/			# Editor only scripts go here
  Plugins/         			# Third-party assets that use the Plugins folder by default
  ThirdPartyAssets/			# Other third party assets, such as 3D models
  ThunderKitSettings/		# Global ThunderKit settings
  GameFiles~/				# Full export of the Subnautica game files, produced using AssetRipper
```

- When creating new folders, keep folder names `PascalCase`.
- Editor-only scripts go under a `Scripts/Editor/` folder within an `Editor` assembly, never mixed with runtime code.
- The Nautilus source code itself is also provided.

## Testing & Validation

- Prefer testable, decoupled logic (pure C# classes, interfaces) so gameplay rules (scoring, collision response, brick durability) can be covered by Unity Test Framework (Edit Mode/Play Mode tests) where practical.
- Validate Odin-exposed fields with `[Required]`/`[ValidateInput]` where a misconfigured reference would cause runtime errors.

## Agent Working Rules

- Do not refactor unrelated code while implementing a feature or fix - only touch what's needed for the agreed change.
- When adding new systems, mirror the existing folder/assembly structure rather than introducing a new pattern.
- When in doubt between "clever/compact" and "clear/idiomatic", choose clear.
- Flag (rather than silently fix) any existing code that violates these conventions, so the user can decide whether to address it.