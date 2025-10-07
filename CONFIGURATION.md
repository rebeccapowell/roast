# Coffee Talk Configuration Guide

This document captures environment variables and settings that influence Coffee Talk's runtime behaviour. It is intended for developers running the stack locally as well as operators deploying the app.

## API (CoffeeTalk.Api)

### Database
- **Connection string**: `ConnectionStrings__coffeetalkdb`
  - Required by the API to connect to Postgres.

### YouTube metadata
The API can enrich submitted videos with details pulled from the YouTube Data API v3. When a key is supplied we will automatically resolve the title and thumbnail for every new submission and persist it on the ingredient.

- **Primary key setting**: `YouTube__ApiKey`
- **Fallback key setting**: `YouTubeApiKey`
  - Either configuration value may be supplied. `YouTube__ApiKey` (bound to the `YouTube` options section) is preferred, while `YouTubeApiKey` is maintained as a backwards compatible override.

When neither value is supplied the application gracefully skips enrichment and the UI will display the raw URL, preserving previous behaviour.

### HTTP client timeouts
The API relies on the default `HttpClient` timeout for outbound calls. If you need to tune it, configure the named client registered for `IYouTubeMetadataProvider` within your hosting environment.

## Web (CoffeeTalk.Web)

- **API base URL**: `NEXT_PUBLIC_API_BASE_URL`
  - Required for the Next.js client to issue API requests and establish the SignalR connection.

## Aspire AppHost

The Aspire host composes the full system for local development. Review the generated `appsettings.Development.json` in `src/CoffeeTalk.AppHost` for per-environment overrides and to ensure the configuration values above are wired through when running locally.
