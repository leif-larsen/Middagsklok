#!/bin/bash
set -e

echo "Building solution..."
dotnet build

echo "Starting API server..."
cd src/Middagsklok.Api
dotnet run --no-build &
API_PID=$!

echo "Waiting for API to start..."
sleep 5

echo "Fetching OpenAPI spec..."
curl -s http://localhost:5000/swagger/v1/swagger.json > ../../contracts/openapi.json

echo "Stopping API server..."
kill $API_PID
wait $API_PID 2>/dev/null || true

echo "✓ OpenAPI spec regenerated at contracts/openapi.json"
