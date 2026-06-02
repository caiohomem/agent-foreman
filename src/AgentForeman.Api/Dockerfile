FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AgentForeman.sln NuGet.Config ./
COPY src/AgentForeman.Api/AgentForeman.Api.csproj src/AgentForeman.Api/
COPY src/AgentForeman.Core/AgentForeman.Core.csproj src/AgentForeman.Core/
COPY src/AgentForeman.Infrastructure/AgentForeman.Infrastructure.csproj src/AgentForeman.Infrastructure/
RUN dotnet restore src/AgentForeman.Api/AgentForeman.Api.csproj

COPY src/ src/
RUN dotnet publish src/AgentForeman.Api/AgentForeman.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AgentForeman.Api.dll"]
