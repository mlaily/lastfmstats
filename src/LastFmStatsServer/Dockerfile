#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["Dapper/nuget.config", "Dapper/"]
COPY ["LastFmStatsServer/LastFmStatsServer.csproj", "LastFmStatsServer/"]
COPY ["RelationalSchema/RelationalSchema.csproj", "RelationalSchema/"]
COPY ["Dapper/Dapper/Dapper.csproj", "Dapper/Dapper/"]
COPY ["Shared/Shared.fsproj", "Shared/"]
RUN dotnet restore "LastFmStatsServer/LastFmStatsServer.csproj"
COPY . .
WORKDIR "/src/LastFmStatsServer"
RUN dotnet build "LastFmStatsServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LastFmStatsServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LastFmStatsServer.dll"]