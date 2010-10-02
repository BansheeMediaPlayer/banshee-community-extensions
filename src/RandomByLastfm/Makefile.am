ASSEMBLY = Banshee.RandomByLastfm
LINK = $(BANSHEE_LIBS) $(BANSHEE_LASTFM_LIBS)
TARGET = library

SOURCES = Banshee.RandomByLastfm/RandomByLastfm.cs

RESOURCES = RandomByLastfm.addin.xml

if ENABLE_RANDOMBYLASTFM
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
