INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(GLIB_CFLAGS) \
	$(GSTREAMER_CFLAGS) \
	$(LIBSAMPLERATE_CFLAGS) \
	$(FFTW3F_CFLAGS)

if ENABLE_LASTFMFINGERPRINT
noinst_PROGRAMS = lastfmfpbridgetest

lastfmfpbridgelibdir = $(EXTENSION_DIR)
lastfmfpbridgelib_LTLIBRARIES = liblastfmfpbridge.la

liblastfmfpbridge_la_LDFLAGS = -avoid-version -module
liblastfmfpbridge_la_SOURCES = \
	CircularArray.h \
	Filter.cpp \
	Filter.h \
	FingerprintExtractor.cpp \
	FingerprintExtractor.h \
	FloatingAverage.h \
	fp_helper_fun.h \
	gst-lastfmfpbridge.cpp \
	gst-lastfmfpbridge.h \
	OptFFT.cpp \
	OptFFT.h

liblastfmfpbridge_la_LIBADD = \
	$(GLIB_LIBS) \
	$(LIBSAMPLERATE_LIBS) \
	$(FFTW3F_LIBS) \
	$(GSTREAMER_LIBS)

lastfmfpbridgetest_SOURCES = \
	lastfmfpbridgetest.cpp
lastfmfpbridgetest_LDADD = \
	liblastfmfpbridge.la \
	$(GSTREAMER_LIBS)

all: $(top_builddir)/bin/liblastfmfpbridge.so

$(top_builddir)/bin/liblastfmfpbridge.so: liblastfmfpbridge.la
	mkdir -p $(top_builddir)/bin
	cp -f .libs/liblastfmfpbridge.so $@
endif

CLEANFILES = $(top_builddir)/bin/liblastfmfpbridge.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(liblastfmfpbridge_la_SOURCES)

