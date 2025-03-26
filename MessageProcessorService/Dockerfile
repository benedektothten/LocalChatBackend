FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Develop
WORKDIR /src
COPY ["MessageProcessorService/MessageProcessorService.csproj", "MessageProcessorService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "MessageProcessorService/MessageProcessorService.csproj"
COPY . .
WORKDIR "/src/MessageProcessorService"
RUN dotnet build "MessageProcessorService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Develop
RUN dotnet publish "MessageProcessorService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --self-contained false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["MessageProcessorService/appsettings.Development.json", "./"]
ENTRYPOINT ["dotnet", "MessageProcessorService.dll"]
