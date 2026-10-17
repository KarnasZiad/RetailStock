using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailStock.Application.Abstractions;
using RetailStock.Application.Queries;
using RetailStock.Infrastructure.Persistence;
using RetailStock.Infrastructure.Persistence.Queries;
using RetailStock.Infrastructure.Persistence.Repositories;

namespace RetailStock.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "RetailStock";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The connection string is resolved lazily so that tests can override the configuration.
        services.AddDbContext<RetailStockDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<RetailStockDbContext>());
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<IStockQueries, StockQueries>();
        services.AddScoped<DatabaseInitializer>();
        return services;
    }
}
