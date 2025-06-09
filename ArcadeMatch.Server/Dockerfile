FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ArcadeMatch.Server/ArcadeMatch.Server.csproj", "ArcadeMatch.Server/"]
RUN dotnet restore "ArcadeMatch.Server/ArcadeMatch.Server.csproj"
COPY . .
WORKDIR "/src/ArcadeMatch.Server"
RUN dotnet build "ArcadeMatch.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ArcadeMatch.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ArcadeMatch.Server.dll"]
