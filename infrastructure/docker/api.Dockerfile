# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY apps/api/*.sln ./apps/api/
COPY apps/api/Directory.Build.props ./apps/api/
COPY apps/api/src/AssetManagement.Domain/*.csproj ./apps/api/src/AssetManagement.Domain/
COPY apps/api/src/AssetManagement.Application/*.csproj ./apps/api/src/AssetManagement.Application/
COPY apps/api/src/AssetManagement.Infrastructure/*.csproj ./apps/api/src/AssetManagement.Infrastructure/
COPY apps/api/src/AssetManagement.Api/*.csproj ./apps/api/src/AssetManagement.Api/
RUN dotnet restore ./apps/api/src/AssetManagement.Api/AssetManagement.Api.csproj

COPY apps/api/src ./apps/api/src
RUN dotnet publish ./apps/api/src/AssetManagement.Api/AssetManagement.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN useradd --uid 5678 --no-create-home appuser
COPY --from=build /app .
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssetManagement.Api.dll"]
