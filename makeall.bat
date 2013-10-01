@ECHO OFF
SETLOCAL ENABLEEXTENSIONS
SETLOCAL ENABLEDELAYEDEXPANSION
SETLOCAL

msbuild > null
IF '%errorlevel%' == '1' GOTO GOTIT

SET FrameworkDir=%windir%\Microsoft.NET\Framework
PATH=%FrameworkDir%64\v3.5;%FrameworkDir%\v3.5;%FrameworkDir%64\v4.0;~f%FrameworkDir%\v4.0*;%PATH%

@echo PATH=%PATH%

VCBuild > null
IF '%errorlevel%' == '1' GOTO GOTIT

echo =======================================
echo Make sure MSBuild.exe is in your PATH 
echo run VCVARS.BAT from VisualStudio 2008 or above
echo =======================================
rem csc /platform:x86 /out:Swicli.Library32 @Swicli.Library.rsp
rem csc /platform:anycpu /out:Swicli.Library.dll @Swicli.Library.rsp
GOTO DONE
)


:GOTIT

IF NOT EXIST "%SWI_HOME_DIR%" (
echo =======================================
echo You must set SWI_HOME_DIR
echo =======================================
GOTO DONE
)

set SWICLIPROJ=SWICLI32.sln
echo %SWI_HOME_DIR% | FINDSTR.EXE "86"
if %errorlevel%==1 set SWICLIPROJ=SWICLI.sln
msbuild %SWICLIPROJ%

:DONE
echo Errorlevel = %errorlevel%
IF '%errorlevel%' NEQ '0' pause
endlocal
