@echo off
echo ==========================================
echo Running All Backend and Frontend Tests...
echo ==========================================

echo [1/2] Running Backend xUnit Tests...
dotnet test backend/Studio.slnx
if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo [2/2] Running Frontend Vitest Tests...
npm --prefix frontend test
if %errorlevel% neq 0 exit /b %errorlevel%

echo.
echo ==========================================
echo All test suites passed successfully!
echo ==========================================
