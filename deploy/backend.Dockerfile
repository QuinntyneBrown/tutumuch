# Builds the three backend services from one restore. Build context: src/backend.
#   docker build -f ../../deploy/backend.Dockerfile --target api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json TuTuMuch.sln ./
COPY TuTuMuch.Domain/TuTuMuch.Domain.csproj TuTuMuch.Domain/
COPY TuTuMuch.Application/TuTuMuch.Application.csproj TuTuMuch.Application/
COPY TuTuMuch.Infrastructure/TuTuMuch.Infrastructure.csproj TuTuMuch.Infrastructure/
COPY TuTuMuch.Api/TuTuMuch.Api.csproj TuTuMuch.Api/
COPY TuTuMuch.Worker/TuTuMuch.Worker.csproj TuTuMuch.Worker/
COPY TuTuMuch.Migrator/TuTuMuch.Migrator.csproj TuTuMuch.Migrator/
RUN dotnet restore TuTuMuch.Api/TuTuMuch.Api.csproj \
 && dotnet restore TuTuMuch.Worker/TuTuMuch.Worker.csproj \
 && dotnet restore TuTuMuch.Migrator/TuTuMuch.Migrator.csproj
COPY . .
RUN dotnet publish TuTuMuch.Api/TuTuMuch.Api.csproj -c Release --no-restore -o /out/api \
 && dotnet publish TuTuMuch.Worker/TuTuMuch.Worker.csproj -c Release --no-restore -o /out/worker \
 && dotnet publish TuTuMuch.Migrator/TuTuMuch.Migrator.csproj -c Release --no-restore -o /out/migrator

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api .
RUN mkdir -p /data/photos && chown app:app /data/photos
ENV ASPNETCORE_URLS=http://+:8080 DOTNET_ENVIRONMENT=Production
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "TuTuMuch.Api.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS worker
WORKDIR /app
COPY --from=build /out/worker .
RUN mkdir -p /data/photos && chown app:app /data/photos
ENV DOTNET_ENVIRONMENT=Production
USER app
ENTRYPOINT ["dotnet", "TuTuMuch.Worker.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS migrator
WORKDIR /app
COPY --from=build /out/migrator .
USER app
ENTRYPOINT ["dotnet", "TuTuMuch.Migrator.dll"]
