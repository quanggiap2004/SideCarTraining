FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SideCar.Worker/SideCar.Worker.csproj", "SideCar.Worker/"]
COPY ["SideCar.Business/SideCar.Business.csproj", "SideCar.Business/"]
RUN dotnet restore "SideCar.Worker/SideCar.Worker.csproj"
COPY . .
WORKDIR "/src/SideCar.Worker"
RUN dotnet build "SideCar.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SideCar.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SideCar.Worker.dll"]
