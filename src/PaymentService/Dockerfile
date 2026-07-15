FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "global.json", "./"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/PaymentService/PaymentService.csproj", "src/PaymentService/"]
RUN dotnet restore "src/PaymentService/PaymentService.csproj"
COPY . .
WORKDIR "/src/src/PaymentService"
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
