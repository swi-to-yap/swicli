#!/bin/bash

export WITH_IKVM="-define:USE_IKVM -r:jpl7,IKVM.OpenJDK.Core,IKVM.OpenJDK.Util,IKVM.Runtime,System.Windows.Forms"

if [ -z "$ODIR" ]; then export ODIR="./lib"; fi
if [ -z "$DMCS_OPTS" ]; then DMCS_OPTS=" -lib:${ODIR}/ -define:PROLOG_SWI -unsafe -warn:0 -r:System.Drawing ${WITH_IKVM}"; fi
if [ -z "$EXTRA_C_FLAGS" ]; then export EXTRA_C_FLAGS="-Wno-unused-result `pkg-config --cflags --libs monosgen-2`"; fi

echo removing previous build
mkdir -p ${ODIR}
find ${ODIR} -ipath "*swicli*"  -not -ipath "*win*" -not -ipath "*Symbols*" -exec rm -f '{}' \;

rm -rf ./src/?*/lib/ ./src/?*/obj/ ./src/?*/bin/ ./src/?*/Debug/ ./src/?*/Release/ ./obj ./src/obj ./src/lib ./src/Debug

# find . -iname "*.so" -or -iname "*.dll" -or -iname "*.pdb" -or -iname "*.lib" -or -iname "*.dll.config" -or -iname "*.pl" -or -iname "*.cffi"  -or -iname "*.exe"


echo doing local C build

export LIBARCH=./lib/x86_64-linux
mkdir -p ${LIBARCH}/
cp src/Swicli.Library/app.config ${LIBARCH}/swicli.dll.config
swipl-ld -m64 src/swicli/swicli.c $EXTRA_C_FLAGS -shared -o ${LIBARCH}/swicli.so


export LIBARCH=./lib/i386-linux
mkdir -p ${LIBARCH}/
cp src/Swicli.Library/app.config ${LIBARCH}/swicli.dll.config
swipl-ld -m32 src/swicli/swicli.c $EXTRA_C_FLAGS -shared -o ${LIBARCH}/swicli.so

mkdir -p lib/amd64/
cp -a ${LIBARCH}/?* lib/amd64/

echo local C build complete!

echo doing local C# build
mkdir -p ${ODIR}
cp src/Swicli.Library/app.config ${ODIR}/Swicli.Library.dll.config

mcs ${DMCS_OPTS} src/Swicli.Library/?*.cs -out:${ODIR}/PInvokeTest
mcs ${DMCS_OPTS} src/Swicli.Library/?*.cs -out:${ODIR}/Swicli.Library.dll
mcs ${DMCS_OPTS}  src/SWICLITestDLL/?*.cs -r:Swicli.Library -out:${ODIR}/SWICLITestDLL.dll
mcs ${DMCS_OPTS}   src/SWICFFITests/?*.cs -r:Swicli.Library -out:${ODIR}/SWICFFITests.exe

mcs -lib:/usr/lib/mono/2.0 -pkg:dotnet src/Example4SWICLI/?*.cs -out:${ODIR}/Example4SWICLI.dll
echo local C# build complete!

rm -rf ./src/?*/lib/ ./src/?*/obj/ ./src/?*/Debug/ ./src/?*/Release/ ./obj ./src/obj ./src/lib ./src/Debug

find . -iname "*.so" -or -iname "*.dll" -or -iname "*.pdb" -or -iname "*.lib" -or -iname "*.dll.config" -or -iname "*.pl" -or -iname "*.cffi"  -or -iname "*.exe"

