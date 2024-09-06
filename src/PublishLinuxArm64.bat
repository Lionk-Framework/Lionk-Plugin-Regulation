@echo off
rem Define variables
set CONFIGURATION=Debug
set RUNTIME_IDENTIFIER=linux-arm64
set TARGET_FRAMEWORK=net8.0
set PUBLISH_DIR=output
set PROJECT_NAME=LionkApp

rem Create project-specific output directory
set PROJECT_OUTPUT_DIR=%PUBLISH_DIR%\%PROJECT_NAME%

rem Raspberry Pi details
set RASPBERRY_PI_USER=pi
set RASPBERRY_PI_HOST=192.168.1.99
set RASPBERRY_PI_DIR=Lionk

rem Publish the application
dotnet publish -c %CONFIGURATION% -r %RUNTIME_IDENTIFIER% -o %PROJECT_OUTPUT_DIR% --self-contained

rem Check if the publish was successful
if %ERRORLEVEL% NEQ 0 (
    echo Publication failed!
    exit /b %ERRORLEVEL%
)

rem Find and copy all .pdb files from the output directory to the Raspberry Pi
for /r %PROJECT_OUTPUT_DIR% %%f in (*.pdb) do (
    echo Copying %%f to Raspberry Pi...
    scp %%f %RASPBERRY_PI_USER%@%RASPBERRY_PI_HOST%:%RASPBERRY_PI_DIR%/
    if %ERRORLEVEL% NEQ 0 (
        echo Failed to copy %%f to Raspberry Pi!
        exit /b %ERRORLEVEL%
    )
)

echo All .pdb files successfully copied to Raspberry Pi!
pause
