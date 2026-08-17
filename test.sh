#!/bin/bash
set -e

echo "=========================================="
echo "Running All Backend and Frontend Tests..."
echo "=========================================="

echo "-> 1/2 Running Backend xUnit Tests..."
dotnet test backend/Studio.slnx

echo "-> 2/2 Running Frontend Vitest Tests..."
npm --prefix frontend test

echo "=========================================="
echo "All test suites completed successfully!"
echo "=========================================="
