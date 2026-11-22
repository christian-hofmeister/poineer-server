FROM mcr.microsoft.com/dotnet/sdk:10.0.201 AS build
ARG VERSION=0.3.0-dev
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/POIneer.Server/POIneer.Server.csproj src/POIneer.Server/
RUN dotnet restore src/POIneer.Server/POIneer.Server.csproj
COPY src/POIneer.Server/ src/POIneer.Server/
RUN dotnet publish src/POIneer.Server/POIneer.Server.csproj --no-restore -c Release -o /app -p:Version="$VERSION"

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "POIneer.Server.dll"]
