FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/SOFIA.DbUp/SOFIA.DbUp.csproj", "src/SOFIA.DbUp/"]
RUN dotnet restore "src/SOFIA.DbUp/SOFIA.DbUp.csproj"

COPY . .
WORKDIR "/src/src/SOFIA.DbUp"
RUN dotnet publish "SOFIA.DbUp.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SOFIA.DbUp.dll"]
