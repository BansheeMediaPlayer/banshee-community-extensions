ASSEMBLY = Banshee.SongKickGeoLocation
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.SongKickGeoLocation.UI/LocalEventsSource.fs \
	Banshee.SongKickGeoLocation/SongKickGeoLocationProvider.fs \
	Banshee.SongKickGeoLocation/SongKickGeoLocationService.fs

RESOURCES =  \
	Resources/city_concerts_logo.png \
	Resources/songkick_logo_300x300.png \
	Resources/FedoraResponseSample.json \
	Resources/GeoResultSample.json \
	Resources/MozillaResponseSample.json \
	Resources/SongKickResponseSample.json \
	SongKickGeoLocation.addin.xml

if ENABLE_SONGKICKGEOLOCATION
include $(top_srcdir)/build/xbuild.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
