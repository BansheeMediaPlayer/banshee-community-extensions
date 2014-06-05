ASSEMBLY = Banshee.SongKickGeoLocation
LINK = $(BANSHEE_LIBS)

SOURCES = Banshee.SongKickGeoLocation/SongKickGeoLocationService.fs

RESOURCES = SongKickGeoLocation.addin.xml

XBUILD = xbuild

include $(top_srcdir)/build/build.mk

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
