# syntax=docker/dockerfile:1

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY backend/SpeedClaim.Api/SpeedClaim.Api.csproj backend/SpeedClaim.Api/
RUN dotnet restore backend/SpeedClaim.Api/SpeedClaim.Api.csproj

COPY backend/SpeedClaim.Api/ backend/SpeedClaim.Api/
RUN dotnet publish backend/SpeedClaim.Api/SpeedClaim.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app

COPY --from=build /app/publish .

# The current storage implementation writes logs and uploads inside the app directory.
# Keep these paths writable for the non-root .NET container user until Blob Storage replaces local uploads.
RUN mkdir -p /app/logs /app/wwwroot/uploads \
    && chown -R app:app /app/logs /app/wwwroot/uploads

USER app

ENTRYPOINT ["dotnet", "SpeedClaim.Api.dll"]
