FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SOFIA.API/SOFIA.API.csproj", "src/SOFIA.API/"]
COPY ["src/SOFIA.Application/SOFIA.Application.csproj", "src/SOFIA.Application/"]
COPY ["src/SOFIA.Domain/SOFIA.Domain.csproj", "src/SOFIA.Domain/"]
COPY ["src/SOFIA.Infrastructure/SOFIA.Infrastructure.csproj", "src/SOFIA.Infrastructure/"]
COPY ["src/SOFIA.SharedKernel/SOFIA.SharedKernel.csproj", "src/SOFIA.SharedKernel/"]

RUN dotnet restore "src/SOFIA.API/SOFIA.API.csproj"

COPY . .
WORKDIR "/src/src/SOFIA.API"
RUN dotnet build "SOFIA.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SOFIA.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Directorio para logs rotativos de Serilog
RUN mkdir -p /app/logs

ENTRYPOINT ["dotnet", "SOFIA.API.dll"]
