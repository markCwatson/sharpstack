FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/App/App.csproj src/App/
RUN dotnet restore src/App/App.csproj

COPY src/App/ src/App/
RUN dotnet publish src/App/App.csproj \
  --configuration Release \
  --no-restore \
  --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

RUN apt-get update \
  && apt-get install --yes --no-install-recommends iproute2 socat \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY scripts/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

EXPOSE 8080

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]