FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DeployPilot.Shared/DeployPilot.Shared.csproj DeployPilot.Shared/
COPY DeployPilot.Artifacts/DeployPilot.Artifacts.csproj DeployPilot.Artifacts/
RUN dotnet restore DeployPilot.Artifacts/DeployPilot.Artifacts.csproj
COPY DeployPilot.Shared DeployPilot.Shared
COPY DeployPilot.Artifacts DeployPilot.Artifacts
RUN dotnet publish DeployPilot.Artifacts/DeployPilot.Artifacts.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "DeployPilot.Artifacts.dll"]
