FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Develop
WORKDIR /src
COPY ["LocalChat/LocalChat.csproj", "LocalChat/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "LocalChat/LocalChat.csproj"
COPY . .
WORKDIR "/src/LocalChat"
RUN dotnet build "LocalChat.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Develop
RUN dotnet publish "LocalChat.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --self-contained false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LocalChat.dll"]
EXPOSE 5000