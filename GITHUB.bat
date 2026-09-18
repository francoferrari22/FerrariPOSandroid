@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"
title FERRARI POS MANAGER - Android + Windows - GitHub
color 0A
cls
echo ================================================================
echo       FERRARI POS MANAGER - PUBLICAR EN GITHUB
echo       ANDROID + WINDOWS - ARTEFACTOS SEPARADOS
echo ================================================================
echo.
echo Este BAT solo te pide el link del repositorio.
echo La rama se establece automaticamente en MAIN.
echo GitHub compilara Android + Windows y entregara TRES ARTEFACTOS INDEPENDIENTES: Android APK, Windows EXE y Windows SETUP.
echo.
set "REPO="
set /p "REPO=PEGÁ EL LINK COMPLETO DE TU REPOSITORIO GITHUB: "
set "REPO=%REPO:"=%"
if not defined REPO (echo ERROR: no se ingreso el link.&pause&exit /b 1)
where git >nul 2>&1
if errorlevel 1 (echo ERROR: Git no esta instalado o no esta en PATH.&pause&exit /b 1)
if exist ".git" rmdir /s /q ".git"
git init -b main >nul 2>&1 || (git init >nul 2>&1 & git branch -M main)
git config user.name "FerrariPOS" >nul
git config user.email "ferraripos@users.noreply.github.com" >nul
git remote add origin "%REPO%" >nul 2>&1 || git remote set-url origin "%REPO%" >nul 2>&1
git add -A
git commit -m "FERRARI POS MANAGER - Android + Windows" >nul 2>&1
echo.
echo Publicando automaticamente en MAIN...
git push -u origin main --force
if errorlevel 1 (
  echo.
  echo Primer intento fallo. Reintentando con HTTP/1.1...
  git -c http.version=HTTP/1.1 push -u origin main --force
)
if errorlevel 1 (echo.&echo ERROR: GitHub rechazo la publicacion.&pause&exit /b 1)
echo.
echo ================================================================
echo PUBLICADO CORRECTAMENTE.
echo GitHub ejecutara Android + Windows automaticamente.
echo Resultado: Android APK + Windows EXE + Windows SETUP
echo ================================================================
pause
endlocal
