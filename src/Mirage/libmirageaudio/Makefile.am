INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(GLIB_CFLAGS) \
	$(GSTREAMER_CFLAGS) \
	$(LIBSAMPLERATE_CFLAGS) \
	$(FFTW3F_CFLAGS)

if ENABLE_MIRAGE
noinst_PROGRAMS = mirageaudiotest

mirageaudiolibdir = $(EXTENSION_DIR)
mirageaudiolib_LTLIBRARIES = libmirageaudio.la

libmirageaudio_la_LDFLAGS = -avoid-version -module
libmirageaudio_la_SOURCES = \
	gst-mirageaudio.c \
	gst-mirageaudio.h

libmirageaudio_la_LIBADD = \
	$(GLIB_LIBS) \
	$(LIBSAMPLERATE_LIBS) \
	$(FFTW3F_LIBS) \
	$(GSTREAMER_LIBS)

mirageaudiotest_SOURCES = \
	mirageaudiotest.c
mirageaudiotest_LDADD = \
	libmirageaudio.la \
	$(GSTREAMER_LIBS)

all: $(top_builddir)/bin/libmirageaudio.so

$(top_builddir)/bin/libmirageaudio.so: libmirageaudio.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/libmirageaudio.so $@
endif

CLEANFILES = $(top_builddir)/bin/libmirageaudio.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(libmirageaudio_la_SOURCES)

