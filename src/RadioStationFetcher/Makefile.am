ASSEMBLY = Banshee.RadioStationFetcher
LINK = $(BANSHEE_LIBS) -r:System.Xml

SOURCES =  \
	Banshee.RadioStationFetcher/FetcherDialog.cs \
	Banshee.RadioStationFetcher/IFreetextSearchable.cs \
	Banshee.RadioStationFetcher/IGenreSearchable.cs \
	Banshee.RadioStationFetcher/RadioStationFetcherService.cs \
	Banshee.RadioStationFetcher/Shoutcast.cs \
	Banshee.RadioStationFetcher/Xiph.cs 

RESOURCES =  \
	RadioStationFetcher.addin.xml \
	Resources/RadioStationFetcherMenu.xml

if ENABLE_RADIOSTATIONFETCHER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
