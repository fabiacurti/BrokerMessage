# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /src

# Copy solution and project files
COPY OrderProcessingSystem.sln ./
COPY src/NotificationService/NotificationService.csproj src/NotificationService/
COPY src/Shared/Shared.csproj src/Shared/

# Restore dependencies
RUN dotnet restore OrderProcessingSystem.sln

# Copy the rest of the source code
COPY src/NotificationService/ src/NotificationService/
COPY src/Shared/ src/Shared/

# Build
RUN dotnet build src/NotificationService/NotificationService.csproj -c Release --no-restore

# Publish
RUN dotnet publish src/NotificationService/NotificationService.csproj -c Release -o /app/publish --no-build

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine

WORKDIR /app

# Copy published app from build stage
COPY --from=build /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "NotificationService.dll"]
