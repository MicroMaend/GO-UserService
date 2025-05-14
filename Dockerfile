# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj og nuget konfiguration
COPY nuget.config ./
COPY nuget-packages ./nuget-packages
COPY GO-UserService/GO-UserService.csproj GO-UserService/

# Restore using local NuGet source
RUN dotnet restore "GO-UserService/GO-UserService.csproj" --configfile ./nuget.config

# Copy the rest of the code
COPY . .

# Build og publish
WORKDIR /src/GO-UserService
RUN dotnet build /src/GO-UserService/GO-UserService.csproj -c Release -o /app/build
RUN dotnet publish /src/GO-UserService/GO-UserService.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GO-UserService.dll"]