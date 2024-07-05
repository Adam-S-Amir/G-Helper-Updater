@echo off
setlocal enabledelayedexpansion

echo Developed by: Adam Amir
echo.

:: Check if running with administrator privileges
NET SESSION >nul 2>&1
if %errorlevel% neq 0 (
    echo This script requires administrator privileges.
    echo Please run the script as an administrator.
    pause
    exit /b 1
)

echo Checking for elevated privileges...
echo Running with administrator privileges.
echo.

REM Specify the process name
set "process_name=GHelper.exe"
echo Searching for GHelper...

REM Use tasklist to check if the process is running
tasklist /fi "imagename eq %process_name%" | find /i "%process_name%" > nul

REM Check the errorlevel to determine if the process is found or not
if %errorlevel% equ 0 (
    echo Process GHelper has been found.
	echo Killing GHelper...
) || (
    echo Process GHelper has not been found.
	echo.
)

REM Kill the process
taskkill /F /IM "%process_name%"
echo.

:: Get the full path of the batch file's directory
set "destination_folder=%~dp0"

REM Specify the file name to search for
set "file_name=GHelper.exe"
echo Searching for GHelper.exe in current folder...

REM Search for the file
for /r "%destination_folder%" %%I in ("%file_name%") do (
	if %errorlevel% equ 0 (
		echo GHelper.exe has been found.
		echo Deleting file...
		del %%~I
		echo Deprecated file deleted successfully.
		echo.
		goto :quit_1
	) else (
		echo GHelper.exe has not been found.
		echo.
		goto :quit_1
	)
)

:quit_1

echo Searching for jq...

REM Check if jqlang.jq is installed
winget show jqlang.jq > nul 2>&1
if %errorlevel% neq 0 (
    REM jqlang.jq is not installed, install it
	echo jq not found, please wait...
    powershell -Command "winget install -e --id jqlang.jq"
) else (
    REM jqlang.jq is already installed
    echo jq has already been installed.
	echo.
)

echo Fetching latest GHelper pre-release...

REM Replace 'owner' and 'repo' with the GitHub owner and repository name
set owner=seerge
set repo=g-helper

REM Fetch the latest release information
for /f "tokens=*" %%i in ('curl -s "https://api.github.com/repos/%owner%/%repo%/releases" ^| jq -r ".[0].assets[1].browser_download_url"') do set download_url=%%i

REM Use curl to fetch the JSON response from the URL
for /f "tokens=*" %%a in ('curl -s "https://api.github.com/repos/seerge/g-helper/releases" ^| jq -r ".[0].tag_name"') do (
	echo Latest pre-release found.
	echo Downloading version "%%~a"...
    goto :quit_2
)

:quit_2

echo Download successful.
echo.
echo Extracting files from archive...
echo -------------------------------------------------------------------------------
REM Download the file
curl -L -o "%destination_folder%\GHelper.zip" %download_url%

REM Change the current directory to the destination folder
cd /d "%destination_folder%"

REM Unzip the file, overwrite existing files
powershell -command "Expand-Archive -Path '.\GHelper.zip' -DestinationPath '.\' -Force"

echo -------------------------------------------------------------------------------
echo.
echo Archive extraction successful.
echo Deleting archive...

REM Delete the zip file
del "GHelper.zip" /q

echo Archive deleted.
echo.
echo Launching GHelper.exe...
REM Start GHelper.exe
start "" "%destination_folder%\GHelper.exe"

REM Check the error level after starting GHelper.exe
if errorlevel 1 (
    echo Failed to start GHelper.exe.
    echo Press any key to exit.
    pause > nul
    exit /b 1
)

echo GHelper.exe started successfully.
exit /b 0