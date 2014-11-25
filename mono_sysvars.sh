#!/bin/bash

export SWI_HOME_DIR=/usr/lib/swi-prolog
export COGBOT_DEV_DIR=/mnt/enki/development/opensim4opencog
export COGBOT_DIR=$COGBOT_DEV_DIR/bin

if [ -f $COGBOT_DEV_DIR/sources/main/swicli/swicli.pl ]; then 
 cp $COGBOT_DEV_DIR/sources/main/swicli/swicli.pl $SWI_HOME_DIR/library
fi


##/usr/local/lib/swipl-5.11.29
export JAVA_HOME=/usr/lib/jvm/jdk1.6.0_10/
#export LD_LIBRARY_PATH=".:$SWI_HOME_DIR/lib:/mnt/enki/development/opensim4opencog/bin:/development/opensim4opencog/lib/x86_64-linux:/usr/lib/jvm/java-1.6.0-openjdk-1.6.0.0.x86_64/jre/lib/amd64/jli:/usr/local/lib/pl-5.6.57/lib/x86_64-linux:/usr/lib/jvm/java-1.6.0-openjdk-1.6.0.0.x86_64/jre/lib/amd64:/usr/lib/jvm/java-1.6.0-openjdk-1.6.0.0.x86_64/jre/lib/amd64/server:/jet6.4-eval/lib/x86/shared"

export LD_LIBRARY_PATH=.:$SWI_HOME_DIR/lib:$COGBOT_DIR:$SWI_HOME_DIR/lib/amd64:$JAVA_HOME/jre/lib/amd64/jli:$SWI_HOME_DIR/lib/x86_64-linux:$JAVA_HOME/jre/lib/amd64:$JAVA_HOME/jre/lib/amd64/server:/usr/lib64:/usr/local/lib64:/usr/lib:/usr/local/lib

export MONO_PATH=.:/usr/lib/ikvm:/usr/lib64/mono/4.0:$LD_LIBRARY_PATH


export JAVA_HOME=/usr/lib/jvm/jdk1.6.0_10
export JAVA_PATH=/usr/java/latest
export PATH=/root/catkin_ws/devel/bin:/opt/ros/indigo/bin:/root/ros_catkin_ws/install_isolated/bin:/usr/java/latest/jre/bin:/usr/java/latest/bin:/usr/lib/qt-3.3/bin:/development/opensim4opencog/bin:/usr/lib/mozart/bin:/root/.nix-profile/bin:/nix/bin:/usr/kerberos/sbin:/usr/kerberos/bin:/opt/JProbe_7.0.3/bin:/jet6.4-eval/bin:/usr/lib/jvm/jdk1.6.0_10//bin:/usr/java/latest/bin:/usr/java/latest/jre/bin:/usr/local/ec2-api-tools-1.3-19403/bin:/usr/lib64/ccache:/opt/acl/acl81.64:/usr/local/lib/acl80.64:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/usr/java/latest/bin:/usr/local/maven-2.0/bin:/opt/slickedit/bin:/root/bin

export LD_LIBRARY_PATH=.:/usr/lib/swi-prolog/lib:/mnt/enki/development/opensim4opencog/bin:/usr/lib/swi-prolog/lib/amd64:/usr/lib/jvm/jdk1.6.0_10//jre/lib/amd64/jli:/usr/lib/swi-prolog/lib/x86_64-linux:/usr/lib/jvm/jdk1.6.0_10/jre/lib/amd64:/usr/lib/jvm/jdk1.6.0_10//jre/lib/amd64/server:/usr/lib64:/usr/local/lib64:/usr/lib:/usr/local/lib

