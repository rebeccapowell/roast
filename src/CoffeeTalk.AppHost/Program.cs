using System;
using System.IO;
using Aspire.Hosting;
using CoffeeTalk.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithVolume("pgdata", "/var/lib/postgresql/data");

var coffeeTalkDb = postgres.AddDatabase("coffeetalkdb");

builder.AddContainer("pgadmin", "dpage/pgadmin4:latest")
    .WithHttpEndpoint(targetPort: 80, name: "http", port: 5050)
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@example.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin")
    .WithBindMount("./pgadmin-servers.json", "/pgadmin4/servers.json")
    .WithVolume("pgadmin-data", "/var/lib/pgadmin")
    .WithReference(postgres);

var migrations = builder.AddProject<Projects.CoffeeTalk_Migrations>("coffeetalk-migrator")
    .WithReference(coffeeTalkDb)
    .WaitFor(coffeeTalkDb);

var api = builder.AddProject<Projects.CoffeeTalk_Api>("api")
    .WithReference(coffeeTalkDb)
    .WaitForCompletion(migrations)
    .WithHttpEndpoint(name: "api-http");

var web = builder.AddProject<Projects.CoffeeTalk_Web>("web")
    .WithHttpEndpoint(name: "web-http", env: "PORT", port: 3000)
    .WithReference(api)
    .WithEnvironment("API_URL", api.GetEndpoint("api-http").Url)
    .WithEnvironment("NEXT_PUBLIC_API_BASE_URL", api.GetEndpoint("api-http").Url)
    .WithHealthCheck("/api/healthz");

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var e2eProjectDir = Path.Combine(repoRoot, "tests", "CoffeeTalk.E2E");
var playwrightArgs = new[]
{
    "test",
    "CoffeeTalk.E2E.csproj",
    "--logger",
    "trx",
};

builder.AddExecutable("playwright", "dotnet", e2eProjectDir, playwrightArgs)
    .WithEnvironment("WEB_BASE_URL", web.GetEndpoint("web-http").Url)
    .WithEnvironment("ASPIRE", "true")
    .WithReference(web)
    .WithReference(api)
    .WithExplicitStart()
    .WithPlaywrightRepeatCommand(playwrightArgs)
    .ExcludeFromManifest()
    .WithParentRelationship(web);

await builder.Build().RunAsync();
