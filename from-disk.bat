SET COGDEV=c:\development\opensim4opencog\current

copy "c:\program files (x86)\pl\bin"\swicli*.* "bin\"
copy "c:\program files (x86)\pl\library"\swicli*.* "library\"
@rem  copy "c:\program files (x86)\pl\lib"\swicli*.* "lib\"
copy "c:\program files\pl\bin"\swicli*.* "bin\"
copy "c:\program files\pl\library"\swicli*.* "library\"
@rem copy "c:\program files\pl\lib"\swicli*.* "lib\"
copy "c:\program files\pl\bin"\swicli.libr*.* "lib\x86_64-linux"
copy "c:\program files\pl\bin"\swicli.libr*.* "lib\i386-linux"


copy %COGDEV%\sources\main\swicli\d*.html .
