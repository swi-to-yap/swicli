#!/bin/bash

export SWI_HOME_DIR=/usr/lib64/swipl-6.0.2
export SWI_HOME_DIR=/usr/lib/swi-prolog
if [ -z "$SWI_HOME_DIR" ]; then echo "set your SWI_HOME_DIR"; exit 1; fi
if [ -z "$SCBINDIR" ]; then export SCBINDIR="./bin"; fi
if [ -z "$DMCS_OPTS" ]; then DMCS_OPTS=" -lib:${SCBINDIR} -unsafe -warn:0 -reference:System.Drawing.dll "; fi


# remove previous system install
rm -f ${PACKSODIR}/Swicli.*  ${PACKSODIR}/swicli*.so
rm -f ${SWI_HOME_DIR}/library/swicli.pl
rm -f ${SWI_HOME_DIR}/library/swicffi.pl


# install this directly
cp prolog/swi*.pl ${SWI_HOME_DIR}/library/
mkdir -p ${SCBINDIR}/
cp -a ${SCBINDIR}/?* ${PACKSODIR}/

export UNKNOWN_SWI="Dont know the platform of SWI-Prolog"
cp prolog/* $SWI_HOME_DIR/library/

if [ -d "$PACKSODIR" ]; then
cp lib/x86_64-linux/* $PACKSODIR
echo "installed 64bit version into $PACKSODIR"
export UNKNOWN_SWI=""
fi


if [ -d "$SWI_HOME_DIR/lib/i386-linux/" ]; then 
cp lib/i386-linux/* $SWI_HOME_DIR/lib/i386-linux/
echo "installed 32bit version"
export UNKNOWN_SWI=""
fi


if [ -d "$SWI_HOME_DIR/lib/x86_64-linux/" ]; then
cp lib/x86_64-linux/* $SWI_HOME_DIR/lib/x86_64-linux/ 
echo "installed 64bit version"
export UNKNOWN_SWI=""
fi

if [ -n "$UNKNOWN_SWI" ]; then
echo $UNKNOWN_SWI
exit 1
fi
