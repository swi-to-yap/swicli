#!/bin/bash

if [ -z "$SCBINDIR" ]; then export SCBINDIR="./bin"; fi
if [ -z "$DMCS_OPTS" ]; then DMCS_OPTS=" -lib:${SCBINDIR}/ -unsafe -warn:0 -reference:System.Drawing.dll "; fi


echo removing previous build
rm -rf ${SCBINDIR}/
mkdir -p ${SCBINDIR}

echo doing local C build
mkdir -p lib/x86_64-linux/
cp src/Swicli.Library/app.config lib/x86_64-linux/swicli.dll.config
swipl-ld -m64 -shared -o lib/x86_64-linux/swicli.so src/swicli/swicli.c `pkg-config --cflags --libs mono-2` -lm
mkdir -p lib/i386-linux/
cp src/Swicli.Library/app.config lib/i386-linux/swicli32.dll.config
swipl-ld -m32 -shared -o lib/i386-linux/swicli32.so src/swicli/swicli32.c `pkg-config --cflags --libs mono-2` -lm

mkdir -p lib/amd64/
cp -a lib/x86_64-linux/?* lib/amd64/

echo doing local C# build
mkdir -p ${SCBINDIR}
cp src/Swicli.Library/app.config ${SCBINDIR}/Swicli.Library.dll.config
dmcs ${DMCS_OPTS} src/Swicli.Library/?*.cs -out:${SCBINDIR}/PInvokeTest.exe
dmcs ${DMCS_OPTS} src/Swicli.Library/?*.cs -out:${SCBINDIR}/Swicli.Library.dll
dmcs ${DMCS_OPTS} src/SWICLITestDLL/?*.cs  -reference:${SCBINDIR}/Swicli.Library.dll -out:${SCBINDIR}/SWICLITestDLL.dll
dmcs ${DMCS_OPTS} src/SWICFFITests/?*.cs -out:${SCBINDIR}/SWICFFITests.exe -reference:${SCBINDIR}/Swicli.Library.dll
mcs -lib:/usr/lib/mono/2.0 -pkg:dotnet src/Example4SWICLI/?*.cs -out:${SCBINDIR}/Example4SWICLI.dll



