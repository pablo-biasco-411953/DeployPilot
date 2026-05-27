FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DeployPilot.Shared/DeployPilot.Shared.csproj DeployPilot.Shared/
COPY DeployPilot.Orchestrator/DeployPilot.Orchestrator.csproj DeployPilot.Orchestrator/
RUN dotnet restore DeployPilot.Orchestrator/DeployPilot.Orchestrator.csproj
COPY DeployPilot.Shared DeployPilot.Shared
COPY DeployPilot.Orchestrator DeployPilot.Orchestrator
RUN dotnet publish DeployPilot.Orchestrator/DeployPilot.Orchestrator.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DeployPilot.Orchestrator.dll"]
