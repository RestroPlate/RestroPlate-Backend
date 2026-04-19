FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["RestroPlate.PublicService/RestroPlate.PublicService.csproj", "RestroPlate.PublicService/"]
COPY ["RestroPlate.EventContracts/RestroPlate.EventContracts.csproj", "RestroPlate.EventContracts/"]
RUN dotnet restore "RestroPlate.PublicService/RestroPlate.PublicService.csproj"

COPY . .
WORKDIR "/src/RestroPlate.PublicService"
RUN dotnet publish "RestroPlate.PublicService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestroPlate.PublicService.dll"]
