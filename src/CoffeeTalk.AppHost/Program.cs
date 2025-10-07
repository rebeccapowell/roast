using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var coffeeTalkDb = postgres.AddDatabase("coffeetalkdb");

builder.AddProject<Projects.CoffeeTalk_Migrations>("coffeetalk-migrator")
    .WithReference(coffeeTalkDb)
    .WaitForCompletion(coffeeTalkDb);

builder.Build().Run();
