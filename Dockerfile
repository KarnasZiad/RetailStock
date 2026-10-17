# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore first (cached layer as long as project files do not change)
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/RetailStock.Domain/RetailStock.Domain.csproj src/RetailStock.Domain/
COPY src/RetailStock.Application/RetailStock.Application.csproj src/RetailStock.Application/
COPY src/RetailStock.Infrastructure/RetailStock.Infrastructure.csproj src/RetailStock.Infrastructure/
COPY src/RetailStock.Api/RetailStock.Api.csproj src/RetailStock.Api/
RUN dotnet restore src/RetailStock.Api/RetailStock.Api.csproj

COPY src/ src/
RUN dotnet publish src/RetailStock.Api/RetailStock.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER app
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=5 \
  CMD wget -qO- http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "RetailStock.Api.dll"]
