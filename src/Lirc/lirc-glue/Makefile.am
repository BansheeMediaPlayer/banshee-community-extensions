if ENABLE_LIRC
lircgluelibdir = $(EXTENSION_DIR)
lircgluelib_LTLIBRARIES = liblircglue.la

liblircglue_la_LDFLAGS = -avoid-version -module
liblircglue_la_SOURCES = \
	lirc-glue.c

liblircglue_la_LIBADD = \
	-llirc_client

all: $(top_builddir)/bin/liblircglue.so

$(top_builddir)/bin/liblircglue.so: liblircglue.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/liblircglue.so $@
endif

CLEANFILES = $(top_builddir)/bin/liblircglue.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(liblircgluelib_la_SOURCES)

