ASSEMBLY = Banshee.EXTENSION-NAME
LINK = $(BANSHEE_LIBS)

SOURCES = Banshee.EXTENSION-NAME/EXTENSION-NAMESource.fs

RESOURCES = EXTENSION-NAME.addin.xml

XBUILD = xbuild

include $(top_srcdir)/build/build.mk

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	$(XBUILD) EXTENSION-NAME.fsproj
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

clean:
	$(XBUILD) EXTENSION-NAME.fsproj /target:Clean

.PHONY: $(ASSEMBLY_FILE)
