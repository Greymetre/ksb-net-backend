#!/bin/sh
set -e

if [ "${SKIP_DB_BOOTSTRAP:-false}" = "true" ]; then
  echo "Skipping database bootstrap."
  exit 0
fi

echo "Running database migrations..."
dotnet Api.dll --migrate

echo "Seeding master data..."
dotnet Api.dll --seed-master-data

echo "Seeding superadmin..."
dotnet Api.dll --seed-superadmin
