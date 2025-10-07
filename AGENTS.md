# Repository Guidelines

## Scope
These guidelines apply to the entire repository unless a more specific `AGENTS.md` overrides them in a subdirectory.

## General Engineering Principles
- Prefer clear, intention-revealing names. Avoid abbreviations unless they are industry standard.
- Favour pure, side-effect-free functions for domain logic when practical. Encapsulate invariants inside the aggregate responsible for them.
- Keep commits and pull requests focused. Update or add automated tests alongside behaviour changes.

## .NET Coding Standards
- Target the latest stable .NET SDK available in the environment; use `nullable enable` in all projects.
- Follow the standard .NET naming conventions (PascalCase for types/methods, camelCase for locals and parameters, etc.).
- Organise code into `src/` and `tests/` folders. Domain projects should avoid direct infrastructure dependencies.
- Use records for immutable value objects and classes for entities/aggregates that manage identity and behaviour.
- Throw `DomainException` (or a more specific domain exception) when invariant violations occur; avoid using generic exceptions for business rules.
- Unit tests should use xUnit with fluent assertions via `Shouldly` where appropriate.

## Next.js Coding Standards
- Use the Next.js App Router with TypeScript and the latest ESLint configuration provided by `create-next-app`.
- Prefer React Server Components for data fetching when feasible and client components only when interactivity is required.
- Keep CSS modularised (CSS Modules, Tailwind, or styled components) and avoid global styles unless necessary.

## Tooling & Environment Setup
- Install the .NET 9 SDK locally using the official install script when the SDK is not preinstalled:
  - `curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh`
  - `chmod +x dotnet-install.sh`
  - `./dotnet-install.sh --version 9.0.100`
- After installation, ensure the current session can find the SDK by exporting the path: `export PATH="$HOME/.dotnet:$PATH"`. Persist the change in your shell profile if you need the SDK in new sessions.
- Always run `dotnet build` and `dotnet test` (and relevant frontend test/build commands when frontend code is touched) before completing work.
- Ensure automated tests cover key business rules described in the PRD, especially around the domain model.
- When updating the Next.js client, run `CI=1 npm run lint` and `CI=1 npm run build` to verify the bundle compiles cleanly.

