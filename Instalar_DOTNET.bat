@echo off
chcp 65001 >nul
title Instalador de Requisitos - Metro Barcelona L1

echo ========================================================
echo   Comprobando requisitos para Metro Barcelona L1...
echo ========================================================
echo.

:: Comprobar si el Runtime de .NET 8 ya esta instalado
dotnet --list-runtimes 2>nul | findstr /C:"Microsoft.WindowsDesktop.App 8." >nul

if %errorlevel% equ 0 (
    echo [OK] .NET 8 Desktop Runtime ya esta instalado en este equipo.
    echo Ya puedes abrir videojuego.exe directamente.
    echo.
    pause
    exit /b 0
)

echo [!] No se ha detectado .NET 8 Desktop Runtime.
echo Iniciando instalacion automatica desde Microsoft...
echo.

winget install --id Microsoft.DotNet.DesktopRuntime.8 --exact --source winget --accept-package-agreements --accept-source-agreements

if %errorlevel% equ 0 (
    echo.
    echo ========================================================
    echo   [OK] Instalacion completada correctamente.
    echo   Ya puedes ejecutar el juego.
    echo ========================================================
) else (
    echo.
    echo [ERROR] No se pudo instalar automaticamente con winget.
    echo Abriendo la pagina oficial de descarga de Microsoft...
    start https://dotnet.microsoft.com/es-es/download/dotnet/8.0
)

echo.
pause