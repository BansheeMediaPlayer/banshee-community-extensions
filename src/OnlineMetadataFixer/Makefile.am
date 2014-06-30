ASSEMBLY = Banshee.OnlineMetadataFixer
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.OnlineMetadataFixer/AcoustIDFingerprintJob.fs \
	Banshee.OnlineMetadataFixer/AcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/AcoustIDStorage.fs \
	Banshee.OnlineMetadataFixer/HelperRecords.fs \
	Banshee.OnlineMetadataFixer/JSonAcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/MissingArtistTitleSource.fs \
	Banshee.OnlineMetadataFixer/OnlineMetadataFixerSource.fs

RESOURCES =  \
	OnlineMetadataFixer.addin.xml \
	Resources/AcoustIDTrackInfo.json

XBUILD = xbuild

if ENABLE_ONLINEMETADATAFIXER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	$(XBUILD) OnlineMetadataFixer.fsproj
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

clean:
	$(XBUILD) OnlineMetadataFixer.fsproj /target:Clean

.PHONY: $(ASSEMBLY_FILE)
