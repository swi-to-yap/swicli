#!/bin/bash

export SWI_HOME_DIR=/usr/lib64/swipl-6.0.2
export SWI_HOME_DIR=/usr/lib/swi-prolog
if [ -z "$SWI_HOME_DIR" ]; then echo "set your SWI_HOME_DIR"; exit 1; fi
if [ -z "$PACKSODIR" ]; then export PACKSODIR="${SWI_HOME_DIR}/lib/amd64"; fi
if [ -z "$SCBUILDIR" ]; then export SCBUILDIR="./lib/amd64"; fi
if [ -z "$DMCS_OPTS" ]; then DMCS_OPTS=" -lib:${SCBUILDIR} -unsafe -warn:0 -reference:System.Drawing.dll "; fi


# remove previous system install
rm -f ${PACKSODIR}/Swicli.*  ${PACKSODIR}/swicli*.so
rm -f ${SWI_HOME_DIR}/library/swicli.pl
rm -f ${SWI_HOME_DIR}/library/swicffi.pl


