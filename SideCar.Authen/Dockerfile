FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SideCar.Authen/SideCar.Authen.csproj", "SideCar.Authen/"]
COPY ["SideCar.Business/SideCar.Business.csproj", "SideCar.Business/"]
RUN dotnet restore "SideCar.Authen/SideCar.Authen.csproj"
COPY . .
WORKDIR "/src/SideCar.Authen"
RUN dotnet build "SideCar.Authen.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SideCar.Authen.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SideCar.Authen.dll"]

