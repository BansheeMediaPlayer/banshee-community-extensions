ASSEMBLY = Banshee.SongKick
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.SongKick.Network/Downloader.cs \
	Banshee.SongKick.Network/SongKickDownloader.cs \
	Banshee.SongKick.Network/SongKickService.cs \
	Banshee.SongKick.Recommendations/Artist.cs \
	Banshee.SongKick.Recommendations/Artists.cs \
	Banshee.SongKick.Recommendations/DisplayAttributes.cs \
	Banshee.SongKick.Recommendations/Event.cs \
	Banshee.SongKick.Recommendations/Events.cs \
	Banshee.SongKick.Recommendations/EventsResultsPage.cs \
	Banshee.SongKick.Recommendations/IResult.cs \
	Banshee.SongKick.Recommendations/Location.cs \
	Banshee.SongKick.Recommendations/Locations.cs \
	Banshee.SongKick.Recommendations/Results.cs \
	Banshee.SongKick.Recommendations/ResultsError.cs \
	Banshee.SongKick.Recommendations/ResultsPage.cs \
	Banshee.SongKick.Recommendations/Utils.cs \
	Banshee.SongKick.Search/PresentSearch.cs \
	Banshee.SongKick.Search/Query.cs \
	Banshee.SongKick.Search/RecommendationProcessor.cs \
	Banshee.SongKick.Search/RecommendationProvider.cs \
	Banshee.SongKick.Search/RecommendedArtist.cs \
	Banshee.SongKick.Search/Search.cs \
	Banshee.SongKick.Search/SearchType.cs \
	Banshee.SongKick.UI/RecommendedArtistsBox.cs \
	Banshee.SongKick.UI/SearchArtistsBox.cs \
	Banshee.SongKick.UI/SearchBar.cs \
	Banshee.SongKick.UI/SearchBox.cs \
	Banshee.SongKick.UI/SearchEventsBox.cs \
	Banshee.SongKick.UI/SearchLocationBox.cs \
	Banshee.SongKick.UI/SearchView.cs \
	Banshee.SongKick.UI/SongKickSourceContents.cs \
	Banshee.SongKick/SongKickCore.cs \
	Banshee.SongKick/SongKickSource.cs

RESOURCES =  \
	Resources/concerts_by_songkick.png \
	Resources/songkick_logo.png \
	SongKick.addin.xml

if ENABLE_SONGKICK
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
