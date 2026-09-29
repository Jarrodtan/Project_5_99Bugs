@echo off
rem [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
rem EcoBalance3D helper for Windows 11 (same commands as tools/unity.sh):
rem   tools\unity.bat open | test | build [android^|windows] | clean
rem Double-click it for a menu instead.
rem Unity is found in the default Unity Hub folder; override with: set UNITY=C:\path\to\Unity.exe
rem Close the Editor before test/build/clean.
setlocal

set "ROOT=%~dp0.."
set "PROJECT=%ROOT%\apps\mobile\EcoBalance3D"
for /f "tokens=2" %%v in ('findstr /b "m_EditorVersion:" "%PROJECT%\ProjectSettings\ProjectVersion.txt"') do set "VERSION=%%v"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\%VERSION%\Editor\Unity.exe"

set "CMD=%~1"
set "PLATFORM=%~2"
set "MENU="
if "%CMD%"=="" goto menu
goto dispatch

:menu
set "MENU=1"
echo.
echo  EcoBalance3D - Unity %VERSION%
echo  ------------------------------
echo   1. Open project in Unity
echo   2. Run EditMode tests
echo   3. Build Android APK
echo   4. Build Windows EXE
echo   5. Clean (delete Library, Temp, Logs, obj, Build)
echo   6. Exit
echo.
set "CHOICE="
set /p "CHOICE=Choose 1-6: "
if "%CHOICE%"=="1" set "CMD=open"
if "%CHOICE%"=="2" set "CMD=test"
if "%CHOICE%"=="3" (set "CMD=build" & set "PLATFORM=android")
if "%CHOICE%"=="4" (set "CMD=build" & set "PLATFORM=windows")
if "%CHOICE%"=="5" set "CMD=clean"
if "%CHOICE%"=="6" exit /b 0
if "%CMD%"=="" (
  echo Invalid choice.
  goto menu
)
if /i not "%CMD%"=="clean" goto dispatch
set "OK="
set /p "OK=Delete the Unity cache - next open re-imports everything. Continue? [y/N] "
if /i "%OK%"=="y" goto dispatch
echo Cancelled.
goto end

:dispatch
if /i "%CMD%"=="clean" goto clean
if /i "%CMD%"=="open" goto check
if /i "%CMD%"=="test" goto check
if /i "%CMD%"=="build" goto check
echo Usage: tools\unity.bat open ^| test ^| build [android^|windows] ^| clean
echo (or double-click it for a menu)
set "RC=0"
goto end

:check
if not exist "%UNITY%" (
  echo Unity %VERSION% not found at: %UNITY%
  echo Install it with Unity Hub, or set UNITY=C:\path\to\Unity.exe
  set "RC=1"
  goto end
)
if /i "%CMD%"=="open" goto open
if /i "%CMD%"=="test" goto test
goto build

:open
start "" "%UNITY%" -projectPath "%PROJECT%"
echo Opening Unity...
set "RC=0"
goto end

:test
echo Running EditMode tests (Unity must be closed, takes a few minutes)...
"%UNITY%" -batchmode -nographics -projectPath "%PROJECT%" -runTests -testPlatform EditMode -testResults "%ROOT%\test-results.xml" -logFile -
set "RC=%ERRORLEVEL%"
if "%RC%"=="0" (echo ALL TESTS PASSED) else (echo TESTS FAILED - see test-results.xml)
echo Results: %ROOT%\test-results.xml
goto end

:build
if "%PLATFORM%"=="" set "PLATFORM=android"
set "METHOD="
if /i "%PLATFORM%"=="android" (
  set "METHOD=BuildScript.BuildAndroid"
  set "TARGET=Android"
)
if /i "%PLATFORM%"=="windows" (
  set "METHOD=BuildScript.BuildWindows"
  set "TARGET=Win64"
)
if not defined METHOD (
  echo Unknown platform %PLATFORM% - use android or windows
  set "RC=1"
  goto end
)
echo Building %PLATFORM% (Unity must be closed, takes a while)...
"%UNITY%" -batchmode -nographics -quit -projectPath "%PROJECT%" -buildTarget %TARGET% -executeMethod %METHOD% -logFile -
set "RC=%ERRORLEVEL%"
if "%RC%"=="0" (echo BUILD OK - output in %PROJECT%\Build\) else (echo BUILD FAILED)
goto end

:clean
for %%d in (Library Temp Logs obj Build) do if exist "%PROJECT%\%%d" (
  rmdir /s /q "%PROJECT%\%%d"
  echo removed %%d
)
if exist "%ROOT%\test-results.xml" del "%ROOT%\test-results.xml"
echo Clean. The next open re-imports everything (a few minutes).
set "RC=0"
goto end

:end
rem Keep the window open when started by double-click.
if defined MENU pause
exit /b %RC%
rem [AI-GENERATED END]
