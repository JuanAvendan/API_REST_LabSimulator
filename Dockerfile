# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore in its own layer so it is cached until a project file changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/LabSimulator.Api/LabSimulator.Api.csproj src/LabSimulator.Api/
RUN dotnet restore src/LabSimulator.Api/LabSimulator.Api.csproj

COPY src/ src/
RUN dotnet publish src/LabSimulator.Api/LabSimulator.Api.csproj -c Release --no-restore -o /app

# Chiseled image: no shell or package manager, runs as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
# TLS terminates at the ingress (Azure Container Apps); trust its X-Forwarded-* headers.
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "LabSimulator.Api.dll"]
