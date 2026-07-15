FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/ShippingService/ShippingService.csproj", "src/ShippingService/"]
RUN dotnet restore "src/ShippingService/ShippingService.csproj"
COPY . .
WORKDIR "/src/src/ShippingService"
RUN dotnet publish "ShippingService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShippingService.dll"]
