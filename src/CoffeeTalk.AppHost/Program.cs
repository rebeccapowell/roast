var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithVolume("pgdata", "/var/lib/postgresql/data");

var coffeeTalkDb = postgres.AddDatabase("coffeetalkdb");

builder.AddContainer("pgadmin", "dpage/pgadmin4:latest")
    .WithHttpEndpoint(targetPort: 80, name: "http", port: 5050)
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@example.com")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "admin")
    .WithReference(postgres);

var migrations = builder.AddProject<Projects.CoffeeTalk_Migrations>("coffeetalk-migrator")
    .WithReference(coffeeTalkDb)
    .WaitFor(coffeeTalkDb);

var api = builder.AddProject<Projects.CoffeeTalk_Api>("coffeetalk-api")
    .WithReference(coffeeTalkDb)
    .WaitForCompletion(migrations);

builder.AddNpmApp("coffeetalk-web", "../CoffeeTalk.Web")
    .WithHttpEndpoint(env: "PORT", port: 3000)
    .WithReference(api)
    .WithEnvironment("NEXT_PUBLIC_API_BASE_URL", api.GetEndpoint("https"));

await builder.Build().RunAsync();
