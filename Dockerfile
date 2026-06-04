FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and all project files first for layer-cached restore
COPY KsbPrBackend.sln ./
COPY src/Api/Api.csproj src/Api/
COPY src/Application/Application.csproj src/Application/
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Shared/Shared.csproj src/Shared/

RUN dotnet restore

# Copy everything else and publish
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

# Create entrypoint script to run migrations and start the app
RUN echo '#!/bin/bash\n\
set -e\n\
echo "Running database migrations..."\n\
dotnet Api.dll --migrate\n\
echo "Starting application..."\n\
exec dotnet Api.dll' > /app/entrypoint.sh && chmod +x /app/entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]

