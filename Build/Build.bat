@echo off

reg.exe query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0" /v MSBuildToolsPath > nul 2>&1
if ERRORLEVEL 1 goto MissingMSBuildRegistry

for /f "skip=2 tokens=2,*" %%A in ('reg.exe query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" /v MSBuildToolsPath') do SET MSBUILDDIR=%%B

IF NOT EXIST %MSBUILDDIR%nul goto MissingMSBuildToolsPath
IF NOT EXIST %MSBUILDDIR%msbuild.exe goto MissingMSBuildExe

"%MSBUILDDIR%msbuild.exe" /version

REM "%MSBUILDDIR%msbuild.exe" ..\Orleans.Providers.MongoDB\Orleans.Providers.MongoDB.sln /t:Build /p:Configuration=Release /p:TargetFramework=v4.0
"%programfiles(x86)%\MSBuild\14.0\Bin\msbuild.exe" ..\Orleans.Providers.MongoDB.sln /t:Build /p:Configuration=Release /p:TargetFramework=v4.0

REM "..\Orleans.Providers.MongoDB\.nuget\nuget.exe" spec "..\Orleans.Providers.MongoDB\bin\Release\Orleans.Providers.MongoDB.dll"
"..\Orleans.Providers.MongoDB\.nuget\nuget.exe" pack Orleans.Providers.MongoDB.dll.nuspec

goto:eof
::ERRORS
::---------------------
:MissingMSBuildRegistry
echo Cannot obtain path to MSBuild tools from registry
goto:eof
:MissingMSBuildToolsPath
echo The MSBuild tools path from the registry '%MSBUILDDIR%' does not exist
goto:eof
:MissingMSBuildExe
echo The MSBuild executable could not be found at '%MSBUILDDIR%'
goto:eof