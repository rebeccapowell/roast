# CoffeeTalk AI Development Guide

## Architecture Overview

This is a multi-room "guess-who-submitted-it" game app using .NET 9 + Next.js, orchestrated by .NET Aspire. Key components:

- **CoffeeTalk.AppHost**: Aspire orchestrator managing the entire distributed app (API, Web, Postgres, migrations)
- **CoffeeTalk.Api**: Web API with minimal APIs and SignalR hub for real-time coordination
- **CoffeeTalk.Web**: Next.js 15 frontend (App Router + TypeScript)
- **CoffeeTalk.Domain**: Rich domain model with aggregates (`CoffeeBar`, `BrewSession`, `BrewCycle`)
- **CoffeeTalk.Infrastructure**: EF Core data layer with separate migration project

## Domain Model Patterns

The codebase follows Domain-Driven Design principles:

- **Aggregates**: `CoffeeBar` is the main aggregate root managing `Hipsters`, `Ingredients`, `Submissions`, and `BrewSessions`
- **Value Objects**: Use records for immutable types like `CoffeeBarCode`, `BrewSessionSummary`
- **Domain Exceptions**: Throw `DomainException` for business rule violations (never generic exceptions)
- **Encapsulation**: Domain objects protect invariants through internal constructors and methods
- **Factory Methods**: Use static `Create()` methods and internal `FromState()` for rehydration

Example domain pattern from `CoffeeBar.cs`:

```csharp
public static CoffeeBar Create(Guid id, string code, string theme, int defaultMaxIngredientsPerHipster, SubmissionPolicy submissionPolicy)
{
    if (defaultMaxIngredientsPerHipster < 1)
        throw new DomainException("Default ingredient quota must be at least one.");
    // ... validation and creation
}
```

## Development Workflow

1. **Start the app**: Run `dotnet run --project src/CoffeeTalk.AppHost` to launch all services via Aspire
2. **Frontend development**: Use `npm run dev` in `src/CoffeeTalk.Web` for Next.js hot reload
3. **Database changes**: Modify `CoffeeTalkDbContext`, create migration in `CoffeeTalk.Migrations`
4. **Testing**: Use `dotnet test` for unit tests; integration tests use Aspire test containers with `[RequiresDockerFact]`

## API Patterns

- **Minimal APIs**: Endpoints are in dedicated static classes (see `BrewSessionEndpoints.MapBrewSessionEndpoints()`)
- **Typed Results**: Use `Results<Ok<T>, NotFound>` return types for clear API contracts
- **Service Registration**: Add services in `Program.cs`, use DI constructor injection
- **Service Defaults**: Common Aspire configuration via `builder.AddServiceDefaults()`

## Testing Conventions

- **Unit Tests**: Use xUnit + Shouldly for assertions (`response.ShouldNotBeNull()`)
- **Integration Tests**: Use `DistributedApplicationTestingBuilder` for full Aspire app testing
- **Docker Dependencies**: Mark tests requiring containers with `[RequiresDockerFact]`
- **Test Structure**: Follow pattern: BuildAndStartAppAsync() → WaitForApiAsync() → Act → Assert

## Next.js Integration

- **App Router**: Use Next.js 15 App Router structure in `src/CoffeeTalk.Web/app/`
- **API Connection**: Frontend connects to backend via environment variable `NEXT_PUBLIC_API_BASE_URL`
- **TypeScript**: Strict TypeScript configuration, prefer Server Components over Client Components
- **Build Process**: Custom start script in `scripts/start.js` for production

## Business Domain Context

This is a multiplayer game where:

- **Coffee Bars** are rooms with 6-character codes (no vowels to avoid words)
- **Hipsters** submit YouTube videos (**Ingredients**) and guess who submitted what
- **Brew Sessions** run **Brew Cycles** showing videos and collecting votes
- **Scoring**: 1 point per correct guess; multiple submitters of same video all count as correct

Key business rules enforced in domain:

- Usernames unique per Coffee Bar (case-insensitive)
- Cannot vote for yourself
- Submission policies: lock on first brew vs always open
- Ingredients consumed once per bar (even if multiple submitters)

## Critical Files to Reference

- `src/CoffeeTalk.Domain/CoffeeBars/CoffeeBar.cs`: Main aggregate with all business logic
- `src/CoffeeTalk.AppHost/Program.cs`: Aspire orchestration and service dependencies
- `prd/prd-01.md`: Complete product requirements and domain rules
- `AGENTS.md`: General .NET and Next.js coding standards for the project

When implementing features, always validate business rules in the domain layer and use the existing patterns for API endpoints, testing, and frontend integration.
