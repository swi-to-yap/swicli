#!/bin/bash

# export SWI_HOME_DIR=/usr/lib64/swipl-6.0.2

if [ -z "$SWI_HOME_DIR" ]; then echo "set your SWI_HOME_DIR"; exit 1; fi
mkdir -p $SWI_HOME_DIR/lib/i386-linux
mkdir -p $SWI_HOME_DIR/lib/x86_64-linux

cp pl/lib/x86_64-linux/* $SWI_HOME_DIR/lib/x86_64-linux/
cp pl/lib/i386-linux/* $SWI_HOME_DIR/lib/i386-linux/
cp pl/library/* $SWI_HOME_DIR/library/


