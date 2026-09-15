@echo off
setlocal
cd /d "%~dp0"
set "REMOTE="
cls
echo ================================================================
echo       FerrariPOS - WINDOWS - PUBLICAR EN GITHUB
echo ================================================================
echo.
set /p "REMOTE=PEGA EL LINK COMPLETO DE TU REPOSITORIO GITHUB: "
if not defined REMOTE exit /b 1
if exist .git rmdir /s /q .git
git init -b windows-build >nul 2>&1 || (git init >nul 2>&1 & git branch -M windows-build >nul 2>&1)
git config user.name "FerrariPOS" >nul
git config user.email "ferraripos@users.noreply.github.com" >nul
git remote add origin "%REMOTE%" >nul 2>&1 || git remote set-url origin "%REMOTE%" >nul 2>&1
git add -A
git commit -m "FerrariPOS Windows" >nul 2>&1
git push -u origin windows-build --force
if errorlevel 1 git -c http.version=HTTP/1.1 push -u origin windows-build --force
if errorlevel 1 (echo ERROR AL PUBLICAR WINDOWS.& pause&exit /b 1)
echo WINDOWS PUBLICADO CORRECTAMENTE.
pause
endlocal
