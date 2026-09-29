@echo off
rem [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
rem EcoBalance helper for Windows 11 (same commands as tools/unity.sh):
rem   tools\unity.bat open | test | build [android^|windows] | clean
rem Unity is found in the default Unity Hub folder; override with: set UNITY=C:\path\to\Unity.exe
rem Close the Editor before test/build.
setlocal

set "ROOT=%~dp0.."
set "PROJECT=%ROOT%\apps\mobile\EcoBalance3D"
for /f "tokens=2" %%v in ('findstr /b "m_EditorVersion:" "%PROJECT%\ProjectSettings\ProjectVersion.txt"') do set "VERSION=%%v"
if not defined UNITY set "UNITY=C:\Program Files\Unity\Hub\Editor\%VERSION%\Editor\Unity.exe"

if /i "%~1"=="clean" goto clean
if /i "%~1"=="open" goto check
if /i "%~1"=="test" goto check
if /i "%~1"=="build" goto check
echo Usage: tools\unity.bat open ^| test ^| build [android^|windows] ^| clean
exit /b 0

:check
if not exist "%UNITY%" (
  echo Unity %VERSION% not found at: %UNITY%
  echo Install it with Unity Hub, or set UNITY=C:\path\to\Unity.exe
  exit /b 1
)
if /i "%~1"=="open" goto open
if /i "%~1"=="test" goto test
goto build

:open
start "" "%UNITY%" -projectPath "%PROJECT%"
exit /b 0

:test
"%UNITY%" -batchmode -nographics -projectPath "%PROJECT%" -runTests -testPlatform EditMode -testResults "%ROOT%\test-results.xml" -logFile -
set "RC=%ERRORLEVEL%"
echo Results: %ROOT%\test-results.xml
exit /b %RC%

:build
set "PLATFORM=%~2"
if "%PLATFORM%"=="" set "PLATFORM=android"
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
  exit /b 1
)
"%UNITY%" -batchmode -nographics -quit -projectPath "%PROJECT%" -buildTarget %TARGET% -executeMethod %METHOD% -logFile -
exit /b %ERRORLEVEL%

:clean
for %%d in (Library Temp Logs obj Build) do if exist "%PROJECT%\%%d" (
  rmdir /s /q "%PROJECT%\%%d"
  echo removed %%d
)
if exist "%ROOT%\test-results.xml" del "%ROOT%\test-results.xml"
echo Clean. The next open re-imports everything (a few minutes).
exit /b 0
rem [AI-GENERATED END]
