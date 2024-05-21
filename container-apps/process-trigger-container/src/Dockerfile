FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["container-apps/process-trigger-container/src/ProcessTriggerContainer.fsproj", "container-apps/process-trigger-container/src/"]
RUN dotnet restore "./container-apps/process-trigger-container/src/ProcessTriggerContainer.fsproj"
COPY . .
WORKDIR "/src/container-apps/process-trigger-container/src"
RUN dotnet build "./ProcessTriggerContainer.fsproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ProcessTriggerContainer.fsproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProcessTriggerContainer.dll"]
