# 1. Use the official .NET 8 SDK to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "StudentApi.csproj"
RUN dotnet publish "StudentApi.csproj" -c Release -o /app/publish

# 2. Use the lightweight .NET 8 Runtime to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# 3. Tell Render to listen on the correct port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "StudentApi.dll"]