using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using RetailStock.Api.Endpoints;
using RetailStock.Api.ErrorHandling;
using RetailStock.Api.Realtime;
using RetailStock.Api.Workers;
using RetailStock.Application;
using RetailStock.Application.Abstractions;
using RetailStock.Infrastructure;
using RetailStock.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IStockEventPublisher, SignalRStockEventPublisher>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RetailStock API",
        Version = "v1",
        Description = "Gestion des stocks multi-magasins en temps reel : receptions, ventes, reservations, inventaires et transferts.",
    });
    options.SupportNonNullableReferenceTypes();
});

builder.Services.AddHealthChecks().AddDbContextCheck<RetailStockDbContext>("postgresql");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.Configure<ReservationExpiryOptions>(builder.Configuration.GetSection(ReservationExpiryOptions.SectionName));
builder.Services.AddHostedService<ReservationExpiryWorker>();
builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection(SimulatorOptions.SectionName));
builder.Services.AddHostedService<StoreActivitySimulator>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "RetailStock API");

app.MapReferenceDataEndpoints();
app.MapStockEndpoints();
app.MapTransferEndpoints();
app.MapHub<StockHub>("/hubs/stock");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seed = app.Configuration.GetValue("Database:SeedDemoData", false);
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(seed);
}

await app.RunAsync();

/// <summary>Entry point, exposed for <c>WebApplicationFactory</c> in integration tests.</summary>
public partial class Program
{
}
