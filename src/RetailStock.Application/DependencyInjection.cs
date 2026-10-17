using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RetailStock.Application.Common;
using RetailStock.Application.Stock;
using RetailStock.Application.Transfers;

namespace RetailStock.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<OperationRunner>();
        services.AddScoped<StockService>();
        services.AddScoped<TransferService>();
        return services;
    }
}
