ASSEMBLY = Banshee.AndroidRemote
LINK = $(BANSHEE_LIBS)

SOURCES = Banshee.AndroidRemote/AndroidRemoteSource.cs

RESOURCES = AndroidRemote.addin.xml

if ENABLE_ANDROIDREMOTE
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
