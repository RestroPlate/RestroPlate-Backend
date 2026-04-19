FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["RestroPlate.InventoryService/RestroPlate.InventoryService.csproj", "RestroPlate.InventoryService/"]
COPY ["RestroPlate.EventContracts/RestroPlate.EventContracts.csproj", "RestroPlate.EventContracts/"]
RUN dotnet restore "RestroPlate.InventoryService/RestroPlate.InventoryService.csproj"

COPY . .
WORKDIR "/src/RestroPlate.InventoryService"
RUN dotnet publish "RestroPlate.InventoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestroPlate.InventoryService.dll"]
