#!/bin/bash

mkdir -p pl/lib/i386-linux
mkdir -p pl/lib/x86_64-linux
mkdir -p pl/library/
swipl-ld -m64 -shared -o pl/lib/x86_64-linux/swicli.so swicli.c `pkg-config --cflags --libs mono-2` -lm
swipl-ld -m32 -shared -o pl/lib/i386-linux/swicli32.so swicli32.c `pkg-config --cflags --libs mono-2` -lm

cp swicli.pl pl/library/
mcs -unsafe -warn:0 Swicli.Library/*.cs -out:Swicli.Library.dll
cp Swicli.Library.dll pl/lib/x86_64-linux/
mv  Swicli.Library.dll pl/lib/i386-linux/

