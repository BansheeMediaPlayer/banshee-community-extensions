ASSEMBLY = Banshee.AlbumArtWriter
LINK = $(BANSHEE_LIBS) -r:System.Drawing

SOURCES =  \
	Banshee.AlbumArtWriter/AlbumArtWriterJob.cs \
	Banshee.AlbumArtWriter/AlbumArtWriterService.cs \
	Banshee.AlbumArtWriter/ConfigurationDialog.cs \
	Banshee.AlbumArtWriter/ConfigurationSchema.cs

RESOURCES =  \
	AlbumArtWriter.addin.xml \
	Resources/GlobalUI.xml
	
if ENABLE_ALBUMARTWRITER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
