FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 1433

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /app
COPY ["NLayersApp.SampleProject/NLayersApp.SampleProject.csproj", "NLayersApp.SampleProject/"]
RUN dotnet restore "NLayersApp.SampleProject/NLayersApp.sampleProject.csproj"
COPY . .
WORKDIR "/src/NLayersApp.SampleProject"
RUN dotnet build "NLayersApp.SampleProject.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NLayersApp.SampleProject.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NLayersApp.SampleProject.dll"]