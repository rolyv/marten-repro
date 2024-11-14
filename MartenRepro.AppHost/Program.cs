var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres", port: 15432)
    .WithImage("postgres", "15")
    .WithPgAdmin()
    .WithDataVolume()
    .AddDatabase("test-single");

var apiService = builder.AddProject<Projects.MartenRepro_ApiService>("api")
    .WithReference(postgres);

builder.Build().Run();
