FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Bedrokia.Cli -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
COPY --from=build /app /app
# server.properties, worlds/, players/, plugins/... sont créés ici : monter un volume pour les conserver.
WORKDIR /data
EXPOSE 19132/tcp 19132-19156/udp
ENTRYPOINT ["dotnet", "/app/bedrokia.dll"]
