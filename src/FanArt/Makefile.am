ASSEMBLY = Banshee.FanArt
LINK = $(BANSHEE_LIBS) \
	-r:../../bin/CacheService.dll \
	-r:FSharp.Core

SOURCES =  \
	Banshee.FanArt.UI/ArtistColumnCell.cs \
	Banshee.FanArt.UI/DoubleColumnRenderer.cs \
	Banshee.FanArt.UI/SingleColumnRenderer.cs \
	Banshee.FanArt/ArtistImageJob.cs \
	Banshee.FanArt/CorrectResults.cs \
	Banshee.FanArt/FanArtArtistImageSpec.cs \
	Banshee.FanArt/FanArtMetadataProvider.cs \
	Banshee.FanArt/FanArtMusicBrainz.cs \
	Banshee.FanArt/FanArtQueryJob.cs \
	Banshee.FanArt/FanArtService.cs \
	Banshee.FanArt/IncorrectResults.cs \
	Banshee.FanArt/Results.cs \
	FanArt/Downloader.cs \
	FanArt/FanArtCore.cs \
	FanArt/FanArtDownloader.cs \
	Utils/IDictionaryUtils.cs

RESOURCES = FanArt.addin.xml

if ENABLE_FANART
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

