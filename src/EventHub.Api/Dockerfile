FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventHub.sln ./
COPY src/EventHub.Domain/EventHub.Domain.csproj src/EventHub.Domain/
COPY src/EventHub.Application/EventHub.Application.csproj src/EventHub.Application/
COPY src/EventHub.Repository/EventHub.Repository.csproj src/EventHub.Repository/
COPY src/EventHub.Infrastructure/EventHub.Infrastructure.csproj src/EventHub.Infrastructure/
COPY src/EventHub.Api/EventHub.Api.csproj src/EventHub.Api/

RUN dotnet restore src/EventHub.Api/EventHub.Api.csproj

COPY src/ src/

RUN dotnet publish src/EventHub.Api/EventHub.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/uploads

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 8080

ENTRYPOINT ["dotnet", "EventHub.Api.dll"]
