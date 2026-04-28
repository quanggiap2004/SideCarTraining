FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SideCar.API/SideCar.API.csproj", "SideCar.API/"]
COPY ["SideCar.Business/SideCar.Business.csproj", "SideCar.Business/"]
RUN dotnet restore "SideCar.API/SideCar.API.csproj"
COPY . .
WORKDIR "/src/SideCar.API"
RUN dotnet build "SideCar.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SideCar.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SideCar.API.dll"]
