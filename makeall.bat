@ECHO OFF
SETLOCAL ENABLEEXTENSIONS
SETLOCAL ENABLEDELAYEDEXPANSION

IF NOT EXIST %SWI_HOME_DIR%  (
echo "SET SWI_HOME_DIR=..."
goto :done
)

set SWICLIPROJ=SWICLI32.sln

echo %SWI_HOME_DIR% | FINDSTR.EXE "86"
@echo on
if %errorlevel%==1 set SWICLIPROJ=SWICLI.sln


msbuild %SWICLIPROJ%
if %errorlevel%==0 goto :done

pause

rem csc /platform:x86 /out:Swicli.Library32 @Swicli.Library.rsp
rem csc /platform:anycpu /out:Swicli.Library.dll @Swicli.Library.rsp


:done
endlocal
