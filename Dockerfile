FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/OrderService.Domain/OrderService.Domain.csproj", "src/OrderService.Domain/"]
COPY ["src/OrderService.Application/OrderService.Application.csproj", "src/OrderService.Application/"]
COPY ["src/OrderService.Infrastructure/OrderService.Infrastructure.csproj", "src/OrderService.Infrastructure/"]
COPY ["src/OrderService.API/OrderService.API.csproj", "src/OrderService.API/"]
COPY ["src/GateWay.API/GateWay.API.csproj", "src/GateWay.API/"]

RUN dotnet restore "src/OrderService.API/OrderService.API.csproj"
RUN dotnet restore "src/GateWay.API/GateWay.API.csproj"

COPY src/ src/

FROM build AS publish-api
RUN dotnet publish "src/OrderService.API/OrderService.API.csproj" -c Release -o /app/api --no-restore

FROM build AS publish-gateway
RUN dotnet publish "src/GateWay.API/GateWay.API.csproj" -c Release -o /app/gateway --no-restore

FROM base AS api
WORKDIR /app
COPY --from=publish-api /app/api .
EXPOSE 5050
ENTRYPOINT ["dotnet", "OrderService.API.dll"]

FROM base AS gateway
WORKDIR /app
COPY --from=publish-gateway /app/gateway .
EXPOSE 5000
ENTRYPOINT ["dotnet", "GateWay.API.dll"]
