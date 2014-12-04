#!/bin/bash

export SWI_HOME_DIR=/usr/lib/swi-prolog
if [ -z "$SWI_HOME_DIR" ]; then echo "set your SWI_HOME_DIR"; exit 1; fi
if [ -z "$PACKSODIR" ]; then export PACKSODIR="${SWI_HOME_DIR}/lib/amd64"; fi
if [ -z "$SCBUILDIR" ]; then export SCBUILDIR="./lib/amd64"; fi
if [ -z "$DMCS_OPTS" ]; then DMCS_OPTS=" -lib:${SCBUILDIR}/ -unsafe -warn:0 -reference:System.Drawing.dll "; fi


echo removing previous build
rm -f ${SCBUILDIR}/Swicli.Library.dll
rm -f ${SCBUILDIR}/PInvokeTest.exe
rm -f ${SCBUILDIR}/SWICFFITests.exe
rm -f ${SCBUILDIR}/swic*.so

echo doing local C build
mkdir -p ${SCBUILDIR}
swipl-ld -m64 -shared -o ${SCBUILDIR}/swicli.so c/swicli/swicli.c `pkg-config --cflags --libs mono-2` -lm
mkdir -p lib/x86_64-linux/
swipl-ld -m64 -shared -o lib/x86_64-linux/swicli.so c/swicli/swicli.c `pkg-config --cflags --libs mono-2` -lm
mkdir -p lib/i386-linux/
swipl-ld -m32 -shared -o lib/i386-linux/swicli32.so c/swicli/swicli32.c `pkg-config --cflags --libs mono-2` -lm


echo doing local C# build
cp c/Swicli.Library/app.config ${SCBUILDIR}/swicli.dll.config
cp c/Swicli.Library/app.config ${SCBUILDIR}/Swicli.Library.dll.config
dmcs ${DMCS_OPTS} c/Swicli.Library/?*.cs -out:${SCBUILDIR}/PInvokeTest.exe
dmcs ${DMCS_OPTS} c/Swicli.Library/?*.cs -out:${SCBUILDIR}/Swicli.Library.dll
dmcs ${DMCS_OPTS} c/SWICLITestDLL/?*.cs  -reference:${SCBUILDIR}/Swicli.Library.dll -out:${SCBUILDIR}/SWICLITestDLL.dll
dmcs ${DMCS_OPTS} c/SWICFFITests/?*.cs -out:${SCBUILDIR}/SWICFFITests.exe -reference:${SCBUILDIR}/Swicli.Library.dll

echo copying complete local C/C# build for packs
cp -a ${SCBUILDIR}/?*.* lib/x86_64-linux/
cp -a ${SCBUILDIR}/?*.* lib/i386-linux/


