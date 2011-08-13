ASSEMBLY = Banshee.AlbumArtWriter
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.AlbumArtWriter/AlbumArtWriterJob.cs \
	Banshee.AlbumArtWriter/AlbumArtWriterService.cs

RESOURCES = AlbumArtWriter.addin.xml

if ENABLE_ALBUMARTWRITER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
