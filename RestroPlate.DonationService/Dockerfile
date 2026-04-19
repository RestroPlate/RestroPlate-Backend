FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["RestroPlate.DonationService/RestroPlate.DonationService.csproj", "RestroPlate.DonationService/"]
COPY ["RestroPlate.EventContracts/RestroPlate.EventContracts.csproj", "RestroPlate.EventContracts/"]
RUN dotnet restore "RestroPlate.DonationService/RestroPlate.DonationService.csproj"

COPY . .
WORKDIR "/src/RestroPlate.DonationService"
RUN dotnet publish "RestroPlate.DonationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestroPlate.DonationService.dll"]
