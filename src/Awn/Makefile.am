ASSEMBLY = Banshee.Awn
LINK = $(BANSHEE_LIBS)

SOURCES = Banshee.Awn/AwnPlugin.cs

RESOURCES = Banshee.Awn.addin.xml

if ENABLE_AWN
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
