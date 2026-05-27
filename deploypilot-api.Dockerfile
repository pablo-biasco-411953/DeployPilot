FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DeployPilot.Shared/DeployPilot.Shared.csproj DeployPilot.Shared/
COPY DeployPilot.Persistence/DeployPilot.Persistence.csproj DeployPilot.Persistence/
COPY DeployPilot.Api/DeployPilot.Api.csproj DeployPilot.Api/
RUN dotnet restore DeployPilot.Api/DeployPilot.Api.csproj
COPY DeployPilot.Shared DeployPilot.Shared
COPY DeployPilot.Persistence DeployPilot.Persistence
COPY DeployPilot.Api DeployPilot.Api
RUN dotnet publish DeployPilot.Api/DeployPilot.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DeployPilot.Api.dll"]
