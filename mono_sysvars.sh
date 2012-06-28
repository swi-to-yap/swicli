#!/bin/bash

export SWI_HOME_DIR=/lib64/swipl-6.0.2
export COGBOT_DEV_DIR=/mnt/enki/development/opensim4opencog
export COGBOT_DIR=$COGBOT_DEV_DIR/bin

if [ -f $COGBOT_DEV_DIR/sources/main/swicli/swicli.pl ]; then 
 cp $COGBOT_DEV_DIR/sources/main/swicli/swicli.pl $SWI_HOME_DIR/library
fi


##/usr/local/lib/swipl-5.11.29
export JAVA_HOME=/usr/lib/jvm/jdk1.6.0_10/
#export LD_LIBRARY_PATH=".:$SWI_HOME_DIR/lib:/mnt/enki/development/opensim4opencog/bin:/development/opensim4opencog/lib/x86_64-linux:/usr/lib/jvm/java-1.6.0-openjdk-1.6.0.0.x86_64/jre/lib/amd64/jli:/usr/local/lib/pl-5.6.57/lib/x86_64-linux:/usr/lib/jvm/java-1.6.0-openjdk-1.6.0.0.x86_64/jre/lib/amd64:/usr/lib/jvm/java-1.6.0-openjdk-1.6.0.0.x86_64/jre/lib/amd64/server:/jet6.4-eval/lib/x86/shared"

export LD_LIBRARY_PATH=.:$SWI_HOME_DIR/lib:$COGBOT_DIR:$SWI_HOME_DIR/lib/x86_64-linux:$JAVA_HOME/jre/lib/amd64/jli:$SWI_HOME_DIR/lib/x86_64-linux:$JAVA_HOME/jre/lib/amd64:$JAVA_HOME/jre/lib/amd64/server:/usr/lib64:/usr/local/lib64:/usr/lib:/usr/local/lib

export MONO_PATH=.:/usr/lib/ikvm:/usr/lib64/mono/4.0:$LD_LIBRARY_PATH
