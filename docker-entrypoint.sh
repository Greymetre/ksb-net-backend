#!/bin/sh
set -e

echo "Starting API..."
exec dotnet Api.dll
