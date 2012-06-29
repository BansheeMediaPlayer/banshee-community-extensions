INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(GLIB_CFLAGS) \
	$(GSTREAMER_CFLAGS)

if ENABLE_VOICECONTROL

voicecontrollibdir = $(EXTENSION_DIR)
voicecontrollib_LTLIBRARIES = libvoicecontrol.la

libvoicecontrol_la_LDFLAGS = -avoid-version -module
libvoicecontrol_la_SOURCES = \
	gst-voicecontrol.c

libvoicecontrol_la_LIBADD = \
	$(GLIB_LIBS) \
	$(GSTREAMER_LIBS)


all: $(top_builddir)/bin/libvoicecontrol.so

$(top_builddir)/bin/libvoicecontrol.so: libvoicecontrol.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/libvoicecontrol.so $@
endif

CLEANFILES = $(top_builddir)/bin/libvoicecontrol.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(libvoicecontrol_la_SOURCES)

