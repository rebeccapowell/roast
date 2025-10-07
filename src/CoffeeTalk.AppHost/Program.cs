var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var coffeeTalkDb = postgres.AddDatabase("coffeetalkdb");

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
