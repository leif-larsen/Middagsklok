FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/Middagsklok.ServiceDefaults/ Middagsklok.ServiceDefaults/
COPY src/Middagsklok.Api/ Middagsklok.Api/

RUN dotnet publish Middagsklok.Api/Middagsklok.Api.csproj \
    -c Release \
    -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Middagsklok.Api.dll"]
