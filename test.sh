#!/bin/bash
set -e

echo "=========================================="
echo "Running All Backend and Frontend Tests..."
echo "=========================================="

# 0. Auto-install frontend dependencies if not present
if [ ! -d "frontend/node_modules" ]; then
    echo "-> Installing frontend dependencies (first-time run)..."
    npm --prefix frontend install
fi

echo "-> 1/2 Running Backend xUnit Tests..."
dotnet test backend/tests/Studio.Tests/Studio.Tests.csproj

echo "-> 2/2 Running Frontend Vitest Tests..."
npm --prefix frontend test

echo "=========================================="
echo "All test suites completed successfully!"
echo "=========================================="

