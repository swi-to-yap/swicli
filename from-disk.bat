SET COGDEV=c:\development\opensim4opencog\current

mkdir "swicli-inst\pl\bin\"
mkdir "swicli-inst\pl\library\"
mkdir "swicli-inst\pl\lib\x86_64-linux"
mkdir "swicli-inst\pl\lib\i386-linux"

copy "c:\program files (x86)\pl\bin"\swicli*.* "swicli-inst\pl\bin\"
copy "c:\program files (x86)\pl\library"\swicli*.* "swicli-inst\pl\library\"
@rem  copy "c:\program files (x86)\pl\lib"\swicli*.* "pl\lib\"
copy "c:\program files\pl\bin"\swicli*.* "swicli-inst\pl\bin\"
copy "c:\program files\pl\library"\swicli*.* "swicli-inst\pl\library\"
@rem copy "c:\program files\pl\lib"\swicli*.* "lib\"
copy "c:\program files\pl\bin"\swicli.libr*.* "swicli-inst\pl\lib\x86_64-linux"
copy "c:\program files\pl\bin"\swicli.libr*.* "swicli-inst\pl\lib\i386-linux"


copy %COGDEV%\sources\main\swicli\d*.html "swicli-inst\"
copy %COGDEV%\sources\main\swicli\RE*.* "swicli-inst\"
copy %COGDEV%\sources\main\swicli\IN*.* "swicli-inst\"


pause
