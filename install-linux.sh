#!/bin/bash

# export SWI_HOME_DIR=/usr/lib64/swipl-6.0.2

if [ -z "$SWI_HOME_DIR" ]; then echo "set your SWI_HOME_DIR"; exit 1; fi

if [ -n "swicli-inst" ]; then 
cd "swicli-inst"
fi

cp pl/library/* $SWI_HOME_DIR/library/

if [ -n "$SWI_HOME_DIR/lib/x86_64-linux/" ]; then
cp pl/lib/x86_64-linux/* $SWI_HOME_DIR/lib/x86_64-linux/ 
echo "installed 64bit version"
exit 0
fi

if [ -n "$SWI_HOME_DIR/lib/i386-linux/" ]; then 
cp pl/lib/i386-linux/* $SWI_HOME_DIR/lib/i386-linux/
echo "installed 32bit version"
exit 0 
fi

echo "Dont know the platform of SWI-Prolog"
exit 1

