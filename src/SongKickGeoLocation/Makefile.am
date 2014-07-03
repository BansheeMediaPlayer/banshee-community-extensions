ASSEMBLY = Banshee.SongKickGeoLocation
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.SongKickGeoLocation.UI/LocalEventsSource.fs \
	Banshee.SongKickGeoLocation/SongKickGeoLocationProvider.fs \
	Banshee.SongKickGeoLocation/SongKickGeoLocationService.fs

RESOURCES =  \
	Resources/city_concerts_logo.png \
	Resources/songkick_logo_300x300.png \
	SongKickGeoLocation.addin.xml

XBUILD = xbuild

if ENABLE_SONGKICKGEOLOCATION
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	$(XBUILD) SongKickGeoLocation.fsproj
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

clean:
	$(XBUILD) SongKickGeoLocation.fsproj /target:Clean

.PHONY: $(ASSEMBLY_FILE)
