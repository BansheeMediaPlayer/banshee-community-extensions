ASSEMBLY = Banshee.Fanart
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.Fanart.UI/ArtistColumnCell.cs \
	Banshee.Fanart.UI/DoubleColumnRenderer.cs \
	Banshee.Fanart.UI/SingleColumnRenderer.cs \
	Banshee.Fanart/ArtistImageJob.cs \
	Banshee.Fanart/CorrectResults.cs \
	Banshee.Fanart/FanartArtistImageSpec.cs \
	Banshee.Fanart/FanartMetadataProvider.cs \
	Banshee.Fanart/FanartMusicBrainz.cs \
	Banshee.Fanart/FanartQueryJob.cs \
	Banshee.Fanart/FanartService.cs \
	Banshee.Fanart/IncorrectResults.cs \
	Banshee.Fanart/Results.cs \
	Fanart/Downloader.cs \
	Fanart/FanartCore.cs \
	Fanart/FanartDownloader.cs \
	Utils/IDictionaryUtils.cs

RESOURCES = Fanart.addin.xml

if ENABLE_FANART
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

