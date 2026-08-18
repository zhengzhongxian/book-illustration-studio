@echo off
echo ==========================================
echo Starting Book Illustration Studio
echo ==========================================

REM Auto-install frontend dependencies if not present
if not exist "frontend\node_modules\" (
    echo [0/2] Installing frontend dependencies first-time run...
    call npm --prefix frontend install
)

echo Starting .NET 8 Backend API on http://localhost:5000
start "Studio.Api" dotnet run --project backend/src/Studio.Api/Studio.Api.csproj --urls "http://localhost:5000"

echo Starting React Vite Frontend on http://localhost:5173
start "Studio.Frontend" npm --prefix frontend run dev

echo Both services launched in separate terminal windows.
echo Frontend: http://localhost:5173
echo Backend API: http://localhost:5000


