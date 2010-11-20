ASSEMBLY = Banshee.ZeitgeistDataprovider
LINK = $(BANSHEE_LIBS) $(ZEITGEIST_SHARP_LIBS)
TARGET = library

SOURCES = Banshee.ZeitgeistDataprovider/ZeitgeistDataprovider.cs

RESOURCES = ZeitgeistDataprovider.addin.xml

if ENABLE_ZEITGEISTDATAPROVIDER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
