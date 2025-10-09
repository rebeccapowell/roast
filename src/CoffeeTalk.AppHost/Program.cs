using Aspire.Hosting;
using CoffeeTalk.AppHost;

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
    .WithHttpEndpoint(env: "http");

var web = builder.AddProject<Projects.CoffeeTalk_Web>("web")
    .WithHttpEndpoint(env: "PORT", port: 3000)
    .WithReference(api)
    .WithEnvironment("API_URL", api.GetEndpoint("https"))
    .WithEnvironment("NEXT_PUBLIC_API_BASE_URL", api.GetEndpoint("https"))
    .WithHealthCheck("/api/healthz");

var e2e = builder.AddProject<Projects.CoffeeTalk_E2E>("e2e")
    .WithEnvironment("WEB_BASE_URL", web.GetEndpoint("http").Url)
    .WithReference(web)
    .WithReference(api)
    .AsManualStart();

await builder.Build().RunAsync();
