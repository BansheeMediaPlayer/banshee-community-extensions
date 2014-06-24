ASSEMBLY = Banshee.OnlineMetadataFixer
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.OnlineMetadataFixer/AcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/HelperRecords.fs \
	Banshee.OnlineMetadataFixer/JSonAcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/OnlineMetadataFixerSource.fs

RESOURCES = OnlineMetadataFixer.addin.xml

XBUILD = xbuild

include $(top_srcdir)/build/build.mk

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
