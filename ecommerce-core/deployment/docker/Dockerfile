# Multi-stage Dockerfile for .NET 8 Application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ECommerce.Web/ECommerce.Web.csproj", "src/ECommerce.Web/"]
COPY ["src/ECommerce.API/ECommerce.API.csproj", "src/ECommerce.API/"]
COPY ["src/ECommerce.Data/ECommerce.Data.csproj", "src/ECommerce.Data/"]

# Restore dependencies
RUN dotnet restore "src/ECommerce.Web/ECommerce.Web.csproj"
RUN dotnet restore "src/ECommerce.API/ECommerce.API.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/ECommerce.Web"
RUN dotnet build "ECommerce.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ECommerce.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "ECommerce.Web.dll"]