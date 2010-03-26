ASSEMBLY = Banshee.ClutterFlow
LINK = $(BANSHEE_LIBS) $(BANSHEE_NOWPLAYING_LIBS) $(CLUTTER_SHARP_LIBS) -r:$(DIR_BIN)/ClutterFlow.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-alarm.schemas.in

SOURCES =  \
	AlbumLoader.cs \
	ArtworkLookup.cs \
	ClutterFlowAlbum.cs \
	ClutterFlowContents.cs \
	ClutterFlowSchemas.cs \
	ClutterFlowService.cs \
	ClutterFlowView.cs \
	FloatingQueue.cs \
	FullscreenButton.cs \
	PartyModeButton.cs

RESOURCES =  \
	ClutterFlow.addin.xml \
	ThemeIcons/22x22/clutterflow-icon.png \
	ThemeIcons/256x256/clutterflow-large.png

if ENABLE_CLUTTERFLOW
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
