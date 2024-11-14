using JasperFx.CodeGeneration.Commands;
using Marten;
using Npgsql;
using Oakton;
using Oakton.Resources;
using Weasel.Core;
using Wolverine;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// builder.AddNpgsqlDataSource("test-single");
builder.AddNpgsqlDataSource("test-multi");

builder.Services.AddMarten(opts =>
    {
        opts.UseSystemTextJsonForSerialization(EnumStorage.AsString, Casing.CamelCase);
        opts.SourceCodeWritingEnabled = false;

        opts.Advanced.MultiHostSettings.ReadSessionPreference = TargetSessionAttributes.PreferStandby;

        // opts.Schema.For<WeatherForecast>();
        opts.Schema.For<WeatherForecast2>();
    }).IntegrateWithWolverine(x => { x.AutoCreate = AutoCreate.CreateOrUpdate; })
    .OptimizeArtifactWorkflow()
    .UseLightweightSessions()
    .UseNpgsqlDataSource()
    .ApplyAllDatabaseChangesOnStartup();
builder.Services.AddResourceSetupOnStartup();

builder.UseWolverine(opts =>
{
    opts.DefaultLocalQueue.TelemetryEnabled(true).UseDurableInbox();
    opts.OptimizeArtifactWorkflow();

    if (!builder.Environment.IsDevelopment()) opts.Services.AssertAllExpectedPreBuiltTypesExistOnStartUp();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapPost("/weatherforecast", async (IDocumentStore store, WeatherForecast forecast) =>
{
    await using var session = store.LightweightSession();
    session.Store(forecast);
    await session.SaveChangesAsync();
    return forecast;
});

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.MapDefaultEndpoints();

return await app.RunOaktonCommands(args);

public class WeatherForecast
{
    private readonly int _temperatureC;

    public WeatherForecast(DateOnly date, int temperatureC, string? summary)
    {
        Id = Guid.NewGuid();
        _temperatureC = temperatureC;
    }

    public Guid Id { get; }
    public int TemperatureF => 32 + (int)(_temperatureC / 0.5556);
}

public class WeatherForecast2
{
    private readonly int _temperatureC;

    public WeatherForecast2(DateOnly date, int temperatureC, string? summary)
    {
        Id = Guid.NewGuid();
        _temperatureC = temperatureC;
    }

    public Guid Id { get; }
    public int TemperatureF => 32 + (int)(_temperatureC / 0.5556);
}