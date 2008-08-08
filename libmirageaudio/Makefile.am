INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(GLIB_CFLAGS) \
	$(GST_CFLAGS) \
	$(LIBSAMPLERATE_CFLAGS) \
	$(FFTW3F_CFLAGS)

noinst_PROGRAMS = mirageaudiotest

mirageaudiolibdir = $(expanded_libdir)
mirageaudiolib_LTLIBRARIES = libmirageaudio.la

libmirageaudio_la_LDFLAGS = -avoid-version -module
libmirageaudio_la_SOURCES = \
	gst-mirageaudio.c \
	gst-mirageaudio.h

libmirageaudio_la_LIBADD = \
	$(GLIB_LIBS) \
	$(LIBSAMPLERATE_LIBS) \
	$(FFTW3F_LIBS) \
	$(GST_LIBS)

mirageaudiotest_SOURCES = \
	mirageaudiotest.c
mirageaudiotest_LDADD = \
	libmirageaudio.la

MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(libmirageaudio_la_SOURCES)

