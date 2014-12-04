# HOW DIO I EMULATE THIS?
mkdir -p lib/x86_64-linux/
swipl-ld -m64 -shared -o lib/x86_64-linux/swicli.so c/swicli/swicli.c `pkg-config --cflags --libs mono-2` -lm
mkdir -p lib/i386-linux/
swipl-ld -m32 -shared -o lib/i386-linux/swicli32.so c/swicli/swicli32.c `pkg-config --cflags --libs mono-2` -lm
mkdir -p lib/amd64/
swipl-ld -m64 -shared -o lib/amd64/swicli.so c/swicli/swicli.c `pkg-config --cflags --libs mono-2` -lm
# CC=swipl-ld

SOBJ=	$(PACKSODIR)/swicli.$(SOEXT)
CFLAGS+= -D_REENTRANT -I/usr/lib/pkgconfig/../../include/mono-2.0  -L/usr/lib/pkgconfig/../../lib -lmono-2.0 -lm -lrt -ldl -lpthread
LIBS= -D_REENTRANT -I/usr/lib/pkgconfig/../../include/mono-2.0  -L/usr/lib/pkgconfig/../../lib -lmono-2.0 -lm -lrt -ldl -lpthread

all:	$(SOBJ)

$(SOBJ): c/swicli/swicli.o
	mkdir -p $(PACKSODIR)
	$(LD) $(LDSOFLAGS) -o $@ $(SWISOLIB) $< $(LIBS)

check::
install::
clean:
	rm -f c/swicli/swicli.o
distclean: clean
	rm -f $(SOBJ)