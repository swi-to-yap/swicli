UNAME := $(shell uname)
ifeq ($(UNAME),$(filter $(UNAME),Linux Darwin SunOS FreeBSD GNU/kFreeBSD NetBSD OpenBSD GNU))
ifeq ($(UNAME),$(filter $(UNAME),Darwin))
OS=darwin
else
ifeq ($(UNAME),$(filter $(UNAME),SunOS))
OS=solaris
else
ifeq ($(UNAME),$(filter $(UNAME),FreeBSD GNU/kFreeBSD NetBSD OpenBSD))
OS=bsd
else
OS=linux
endif
endif
endif
else
OS=windows
endif

HARDWARE_NAME := $(shell uname -m)

ifneq ($(findstring 64, $(HARDWARE_NAME)),)
BITS=""
else
BITS=""
endif


SHELL := /bin/bash
ifndef SWIARCH
SWIARCH=$(shell uname -m)-$(OS)
endif
LIBDIR=lib/$(SWIARCH)
#
#
CC=swipl-ld
INC1=src/swicli
CURRDIR=$(shell pwd)
#INC2=$(shell while read one two three; \
#do TEMP=$two; \
#done <<< `whereis swipl`; \
#readlink -f $TEMP; \
)/include #da completare

CFLAGS=$(shell pkg-config --cflags --libs mono-2)

INC2=$(SWIHOME)/include
#INC2=`echo /usr/lib/swi*`/include/
INCDIRS= -I$(INC1) -I$(INC2)

LDFLAGS= $(CFLAGS) -fPIC -DBP_FREE -O3 -fomit-frame-pointer -Wall -g -O2 $(INCDIRS) 

ifndef SOEXT
SOEXT=so
endif
#
#
# You shouldn't need to change what follows.
#
#src/swicli32/.libs/libcudd-3.0.0.so.0.0.0

#
SWICLI_SO=$(LIBDIR)/swicli.$(SOEXT)

all: $(SWICLI_SO)
	@echo $(shell ./make-linux.sh) \
	$(CC) -shared -Wno-unused-result src/swicli/swicli.c $(LDFLAGS) $(MONO_FLAGS) -o $(SWICLI_SO)
	
    
#-Wl,-R,$(YAPLIBDIR) -Wl,-R,$(LIBDIR)
#  $(CC) -export-dynamic swicli4.o  $(LDFLAGS) -o $(SWICLI_SO) ;\
# swicli64.o : src/swicli64/swicli64.c $(CC) -c $(CFLAGSSWICLI64) src/swicli64/swicli64.c -o swicli64.o


distclean: clean
	@echo rm Makefile.bak

clean:
	rm -f $(SWICLI_SO)

check:
	@echo "no check"

install: all
	cp $(SWICLI_SO) $(LIBDIR)


