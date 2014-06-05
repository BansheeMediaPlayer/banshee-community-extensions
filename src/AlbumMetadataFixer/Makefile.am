ASSEMBLY = Banshee.AlbumMetadataFixer
LINK = $(BANSHEE_LIBS)

SOURCES = Banshee.AlbumMetadataFixer/AlbumMetadataFixerSource.fs

RESOURCES = AlbumMetadataFixer.addin.xml

XBUILD = xbuild

if ENABLE_ALBUMMETADATAFIXER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	$(XBUILD) AlbumMetadataFixer.fsproj
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

clean:
	$(XBUILD) AlbumMetadataFixer.fsproj /target:Clean

.PHONY: $(ASSEMBLY_FILE)
