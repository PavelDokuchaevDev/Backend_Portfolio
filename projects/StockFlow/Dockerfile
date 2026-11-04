FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY Directory.Build.props global.json ./
COPY projects/StockFlow/ projects/StockFlow/
RUN dotnet publish projects/StockFlow/src/StockFlow.Api -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StockFlow.Api.dll"]
