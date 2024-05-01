FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ["src/MyClient/MyClient.csproj", "MyClient/"]
RUN dotnet restore "MyClient/MyClient.csproj"
COPY src/ .
WORKDIR "/MyClient"
RUN dotnet build "MyClient.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyClient.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyClient.dll"]