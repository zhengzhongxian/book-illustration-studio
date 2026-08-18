@echo off
echo ==========================================
echo Running All Backend and Frontend Tests...
echo ==========================================

:: Auto-install frontend dependencies if not present
if not exist "frontend\node_modules\" (
    echo [0/2] Installing frontend dependencies (first-time run)...
    call npm --prefix frontend install
)

echo [1/2] Running Backend xUnit Tests...
dotnet test backend/tests/Studio.Tests/Studio.Tests.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo [2/2] Running Frontend Vitest Tests...
npm --prefix frontend test
if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo ==========================================
echo All test suites passed successfully!
echo ==========================================

