#!/bin/bash

rm -rf swicli-inst
rm -f /usr/lib/swi-prolog/lib/amd64/Swicli.Library.*  /usr/lib/swi-prolog/lib/amd64/swicli*.so
rm -f /usr/lib/swi-prolog/library/swicli.pl
rm -f /usr/lib/swi-prolog/library/swicffi.pl

mkdir -p swicli-inst/pl/lib/i386-linux
mkdir -p swicli-inst/pl/lib/x86_64-linux
mkdir -p swicli-inst/pl/library/
swipl-ld -m64 -shared -o swicli-inst/pl/lib/x86_64-linux/swicli.so swicli.c `pkg-config --cflags --libs mono-2` -lm
swipl-ld -m32 -shared -o swicli-inst/pl/lib/i386-linux/swicli32.so swicli32.c `pkg-config --cflags --libs mono-2` -lm

cp swi*.pl swicli-inst/pl/library/
rm -f Swicli.Library.dll
rm -f Swicli.Library.exe
dmcs -unsafe -warn:0 Swicli.Library/*.cs -out:PInvokeTest.exe
dmcs -unsafe -warn:0 Swicli.Library/*.cs -out:Swicli.Library.dll
cp Swicli.Library.dll swicli-inst/pl/lib/x86_64-linux/
mv  Swicli.Library.dll swicli-inst/pl/lib/i386-linux/

cp d*.html swicli-inst/
cp RE*.* swicli-inst/
cp in*.* swicli-inst/


