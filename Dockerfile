# ---------- Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SPIn.csproj ./
RUN dotnet restore SPIn.csproj

COPY . .
RUN dotnet publish SPIn.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/spin.db" \
    App__RutaClavesProteccion=/app/data/keys \
    App__RedirigirHttps=false

# Entrega 8: ML.NET (pronóstico SSA) usa Intel MKL, que en Linux necesita la librería OpenMP de GNU.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*

# La carpeta de datos pertenece al usuario sin privilegios de la imagen oficial ($APP_UID).
RUN mkdir -p /app/data/keys && chown -R $APP_UID:$APP_UID /app/data

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "SPIn.dll"]
