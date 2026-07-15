FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "global.json", "./"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/NotificationService/NotificationService.csproj", "src/NotificationService/"]
RUN dotnet restore "src/NotificationService/NotificationService.csproj"
COPY . .
WORKDIR "/src/src/NotificationService"
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]
