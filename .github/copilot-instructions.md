# CoffeeTalk Repository Guide for AI Agents

This guide provides essential context for working within the CoffeeTalk repository.

## Architecture Overview

The repository is a monorepo containing a .NET backend and a Next.js frontend, orchestrated with .NET Aspire.

- **.NET Backend (`src/`):** Follows a Domain-Driven Design (DDD) approach.

  - `CoffeeTalk.Domain`: Contains the core business logic and entities. The central aggregate is `CoffeeBar` (`src/CoffeeTalk.Domain/CoffeeBars/CoffeeBar.cs`), which manages the state of a coffee bar session.
  - `CoffeeTalk.Api`: Exposes a web API and uses SignalR for real-time updates via `CoffeeBarHub` (`src/CoffeeTalk.Api/Hubs/CoffeeBarHub.cs`).
  - `CoffeeTalk.AppHost`: Configures and launches the various .NET projects and the Next.js frontend for local development.
  - `CoffeeTalk.ServiceDefaults`: Provides shared configurations for .NET projects.

- **Next.js Frontend (`src/CoffeeTalk.Web/`):**
  - A TypeScript application using the App Router.
  - Communicates with the .NET API for data and uses a SignalR client to receive real-time updates.
  - Key components are located in `src/CoffeeTalk.Web/src/app/`. For example, the main coffee bar interface is in `src/CoffeeTalk.Web/src/app/coffee-bars/[code]/`.
  - Components are organized into server components (default) and client components (with `"use client"` directive) and will be found in `src/CoffeeTalk.Web/src/components/`.
  - Utils and hooks are in `src/CoffeeTalk.Web/src/lib/` and `src/CoffeeTalk.Web/src/hooks/`.
  - Styles that belong to specific components are co-located with them.
  - Functional components are preferred over class components.
  - make use of `useActionState` for state management.
  - types are defined in `src/CoffeeTalk.Web/src/types/`.
  - constant values are defined in `src/CoffeeTalk.Web/src/constants/`.
  - Server-side data fetching and manipulation is done in `src/CoffeeTalk.Web/src/actions/`.
  - keep DRY principles in mind.
  - API calls are made using the `fetch` API.
  - use approuter features like layouts, templates, and error handling. directories under `src/CoffeeTalk.Web/src/app/` represent routes and sub-routes. if a directory or its children contains a `page.tsx` file, it corresponds to a route.
  - use the alias `@/` to reference the `src/` directory.

## Developer Workflow

- **Running the Application:** The easiest way to run the entire application is to launch the `CoffeeTalk.AppHost` project. This will start all backend services and the Next.js frontend.

- **.NET Development:**

  - Build the solution with `dotnet build`.
  - Run tests with `dotnet test`.
  - Adhere to the existing DDD patterns. Business rules are enforced in domain entities and services. Invariant violations should throw a `DomainException`.
  - Use records for immutable data transfer objects (DTOs) and value objects.

- **Next.js Development:**
  - Navigate to `src/CoffeeTalk.Web`.
  - Install dependencies with `npm install`.
  - Run the development server with `npm run dev`.
  - Before committing, run `CI=1 npm run lint` and `CI=1 npm run build`.
  - Prefer server components for data fetching and client components for interactivity.

## Key Patterns & Conventions

- **Real-time Communication:** The frontend and backend are connected via SignalR. When a user interacts with a coffee bar, the backend processes the logic and broadcasts updates to all connected clients via the `CoffeeBarHub`.
- **Coffee Bar Codes:** Coffee bars are identified by a unique code, which is part of the URL (e.g., `/coffee-bars/XYZ-123`). The `CoffeeBarCodeGenerator.cs` service is responsible for creating these codes.
- **YouTube Integration:** The application integrates with YouTube. The `YouTubeMetadataProvider.cs` and `YouTubeVideoIdParser.cs` are used to fetch and handle video metadata.
