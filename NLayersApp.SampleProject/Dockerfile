FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["NLayersApp.Controllers/NLayersApp.Controllers.csproj", "NLayersApp.Controllers/"]
RUN dotnet restore "NLayersApp.Controllers/NLayersApp.Controllers.csproj"
COPY . .
WORKDIR "/src/NLayersApp.Controllers"
RUN dotnet build "NLayersApp.Controllers.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NLayersApp.Controllers.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NLayersApp.Controllers.dll"]