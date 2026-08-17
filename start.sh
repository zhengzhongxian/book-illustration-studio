#!/bin/bash
set -e

echo "=========================================="
echo "Starting Book Illustration Studio..."
echo "=========================================="

# 1. Start Backend in background
echo "-> Launching .NET 8 Backend API on http://localhost:5000..."
dotnet run --project backend/src/Studio.Api/Studio.Api.csproj --urls "http://localhost:5000" &
BACKEND_PID=$!

# 2. Start Frontend
echo "-> Launching React Vite Frontend on http://localhost:5173..."
npm --prefix frontend run dev &
FRONTEND_PID=$!

trap "kill $BACKEND_PID $FRONTEND_PID 2>/dev/null" EXIT

echo "App running. Press Ctrl+C to stop."
wait
