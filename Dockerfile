# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["FoodHub.WebAPI/FoodHub.WebAPI.csproj", "FoodHub.WebAPI/"]
COPY ["FoodHub.Infrastructure/FoodHub.Infrastructure.csproj", "FoodHub.Infrastructure/"]
COPY ["FoodHub.Application/FoodHub.Application.csproj", "FoodHub.Application/"]
COPY ["FoodHub.Domain/FoodHub.Domain.csproj", "FoodHub.Domain/"]

RUN dotnet restore "FoodHub.WebAPI/FoodHub.WebAPI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/FoodHub.WebAPI"
RUN dotnet build "FoodHub.WebAPI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "FoodHub.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "FoodHub.WebAPI.dll"]