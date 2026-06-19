FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OnlineRestaurantApp/OnlineRestaurantApp.csproj", "OnlineRestaurantApp/"]
COPY ["OnlineRestaurantApp.Dal/OnlineRestaurantApp.Dal.csproj", "OnlineRestaurantApp.Dal/"]
COPY ["OnlineRestaurantApp.Components/OnlineRestaurantApp.Components.csproj", "OnlineRestaurantApp.Components/"]
RUN dotnet restore "OnlineRestaurantApp/OnlineRestaurantApp.csproj"
COPY . .
WORKDIR "/src/OnlineRestaurantApp"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OnlineRestaurantApp.dll"]