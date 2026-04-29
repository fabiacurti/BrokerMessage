# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

# Copy solution and project files
COPY OrderProcessingSystem.sln ./
COPY src/OrderApi/OrderApi.csproj src/OrderApi/
COPY src/Shared/Shared.csproj src/Shared/

# Restore dependencies
RUN dotnet restore OrderProcessingSystem.sln

# Copy the rest of the source code
COPY src/OrderApi/ src/OrderApi/
COPY src/Shared/ src/Shared/

# Build
RUN dotnet build src/OrderApi/OrderApi.csproj -c Release --no-restore

# Publish
RUN dotnet publish src/OrderApi/OrderApi.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "OrderApi.dll"]
