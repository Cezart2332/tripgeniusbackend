# ─── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY TripGeniusBackend.API/TripGeniusBackend.API.csproj TripGeniusBackend.API/
COPY TripGeniusBackend.Application/TripGeniusBackend.Application.csproj TripGeniusBackend.Application/
COPY TripGeniusBackend.Domain/TripGeniusBackend.Domain.csproj TripGeniusBackend.Domain/
COPY TripGeniusBackend.Infrastructure/TripGeniusBackend.Infrastructure.csproj TripGeniusBackend.Infrastructure/

RUN dotnet restore TripGeniusBackend.API/TripGeniusBackend.API.csproj

COPY . .

RUN dotnet publish TripGeniusBackend.API/TripGeniusBackend.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Runtime Stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TripGeniusBackend.API.dll"]