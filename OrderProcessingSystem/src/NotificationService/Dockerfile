# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /app

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore

# Build
RUN dotnet build src/NotificationService/NotificationService.csproj -c Release --no-restore

# Publish
RUN dotnet publish src/NotificationService/NotificationService.csproj -c Release -o /publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

WORKDIR /app

# Copy published app from build stage
COPY --from=build /publish .

# Run the application
ENTRYPOINT ["dotnet", "NotificationService.dll"]
