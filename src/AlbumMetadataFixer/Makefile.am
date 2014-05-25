ASSEMBLY = Banshee.AlbumMetadataFixer
LINK = $(BANSHEE_LIBS)

SOURCES = Banshee.AlbumMetadataFixer/AlbumMetadataFixerSource.cs

RESOURCES = AlbumMetadataFixer.addin.xml

if ENABLE_ALBUMMETADATAFIXER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
