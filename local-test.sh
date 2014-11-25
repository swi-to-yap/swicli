killall -9 swipl
fg
#!/bin/bash
. ./mono_sysvars.sh
./make-linux.sh
./install-linux.sh
# swipl -g "use_module(library(swicli))."
swipl -g "use_module(library(swicffi)),cffi_tests."

