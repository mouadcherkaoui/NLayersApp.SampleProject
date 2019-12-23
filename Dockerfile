FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
WORKDIR /src

COPY *.sln ./
COPY Nuget.Config ./
COPY NLayersApp.SampleProject/appsettings.json ./
COPY NLayersApp.SampleProject/NLayersApp.SampleProject.csproj NLayersApp.SampleProject/
COPY NLayersApp.Models/NLayersApp.Models.csproj NLayersApp.Models/
COPY NLayersApp.SampleProject.Tests/NLayersApp.SampleProject.Tests.csproj NLayersApp.SampleProject.Tests/

RUN dotnet restore --configfile Nuget.Config

COPY . .
WORKDIR /src/NLayersApp.SampleProject
RUN dotnet build -c Release -o /app

FROM builder AS publish
RUN dotnet publish -c Release -o /app

FROM base AS production
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "NLayersApp.SampleProject.dll"]