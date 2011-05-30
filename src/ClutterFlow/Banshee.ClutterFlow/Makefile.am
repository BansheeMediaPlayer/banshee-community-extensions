ASSEMBLY = Banshee.ClutterFlow
LINK = $(BANSHEE_LIBS) $(BANSHEE_NOWPLAYING_LIBS) $(CLUTTER_SHARP_LIBS) -r:$(DIR_BIN)/ClutterFlow.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-alarm.schemas.in

SOURCES =  \
	AlbumComparer.cs \
	AlbumLoader.cs \
	ArtworkLookup.cs \
	ClutterFlowAlbum.cs \
	ClutterFlowContents.cs \
	ClutterFlowSchemas.cs \
	ClutterFlowService.cs \
	ClutterFlowView.cs \
	FloatingQueue.cs \
	FullscreenButton.cs \
	IndexedQueue.cs \
	PartyModeButton.cs \
	SortButton.cs

RESOURCES =  \
	ClutterFlow.addin.xml \
	ThemeIcons/22x22/clutterflow-icon.png \
	ThemeIcons/256x256/clutterflow-large.png

EXTRA_BUNDLE = $(CLUTTER_BUNDLEFILES)

if ENABLE_CLUTTERFLOW
include $(top_srcdir)/build/build.mk

define EXTRA_INSTALL_DATA_HOOK
	for ASM in $(CLUTTER_BUNDLEFILES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;
endef

define EXTRA_UNINSTALL_HOOK
	for ASM in $(CLUTTER_BUNDLEFILES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;
endef

else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
