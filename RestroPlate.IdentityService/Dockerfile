FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["RestroPlate.IdentityService/RestroPlate.IdentityService.csproj", "RestroPlate.IdentityService/"]
RUN dotnet restore "RestroPlate.IdentityService/RestroPlate.IdentityService.csproj"

COPY . .
WORKDIR "/src/RestroPlate.IdentityService"
RUN dotnet publish "RestroPlate.IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RestroPlate.IdentityService.dll"]
