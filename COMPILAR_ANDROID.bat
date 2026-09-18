@echo off
setlocal EnableExtensions DisableDelayedExpansion
cd /d "%~dp0"
set "LOG=%~dp0PUBLICACION_ANDROID_GITHUB_ERROR.txt"
set "REMOTE="
set "FAILED=0"

cls
echo ================================================================
echo          FERRARI POS MANAGER - ANDROID
echo             PUBLICAR EN GITHUB
echo ================================================================
echo.
echo Este BAT solo necesita el link de tu repositorio GitHub.
echo Publicara automaticamente en MAIN y ejecutara el workflow Android.
echo.

if not exist "%~dp0FerrariPOS.Manager.Android\settings.gradle.kts" (
  echo ERROR: no encuentro FerrariPOS.Manager.Android.
  echo Ejecuta este BAT desde la carpeta raiz del proyecto.
  pause
  exit /b 1
)
where git >nul 2>&1
if errorlevel 1 (
  echo ERROR: Git no esta instalado o no esta disponible en PATH.
  pause
  exit /b 1
)

set /p "REMOTE=PEGA EL LINK COMPLETO DE TU REPOSITORIO GITHUB: "
set "REMOTE=%REMOTE:"=%"
if not defined REMOTE (
  echo ERROR: no se ingreso el link.
  pause
  exit /b 1
)

>"%LOG%" echo FerrariPOS - Publicacion Android GitHub
>>"%LOG%" echo Fecha: %date% %time%
>>"%LOG%" echo Repositorio: %REMOTE%

if exist ".git" rmdir /s /q ".git"
git init -b main >>"%LOG%" 2>&1
if errorlevel 1 (
  git init >>"%LOG%" 2>&1
  git branch -M main >>"%LOG%" 2>&1
)
git config user.name "FerrariPOS" >>"%LOG%" 2>&1
git config user.email "ferraripos@users.noreply.github.com" >>"%LOG%" 2>&1

git remote add origin "%REMOTE%" >>"%LOG%" 2>&1
if errorlevel 1 git remote set-url origin "%REMOTE%" >>"%LOG%" 2>&1

git add -A >>"%LOG%" 2>&1
git commit -m "FERRARI POS MANAGER Android - compilacion automatica" >>"%LOG%" 2>&1
if errorlevel 1 (
  echo ERROR al crear el commit. Revisa:
  echo %LOG%
  pause
  exit /b 1
)

echo.
echo Publicando automaticamente en MAIN...
git push -u origin main --force >>"%LOG%" 2>&1
if errorlevel 1 (
  echo Primer envio fallo. Reintentando...
  git -c http.version=HTTP/1.1 push -u origin main --force >>"%LOG%" 2>&1
)
if errorlevel 1 (
  echo.
  echo ERROR: GitHub rechazo la publicacion.
  echo Detalle: %LOG%
  pause
  exit /b 1
)

echo.
echo ================================================================
echo PUBLICACION COMPLETADA
echo ================================================================
echo.
echo GitHub ejecutara automaticamente:
echo FERRARI POS MANAGER - Android
 echo.
echo No se te pedira MAIN ni ninguna otra opcion.
echo El unico dato que pide este BAT es el link del repositorio.
echo.
echo Registro: %LOG%
pause
endlocal
