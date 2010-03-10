ASSEMBLY = Banshee.ClutterFlow
LINK = $(BANSHEE_LIBS) $(BANSHEE_NOWPLAYING_LIBS) $(CLUTTER_SHARP_LIBS) -r:$(DIR_BIN)/ClutterFlow.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-alarm.schemas.in

SOURCES =  \
    ClutterFlowService.cs \
    ClutterFlowContents.cs \
    ClutterFlowSchemas.cs \
    ClutterFlowAlbum.cs \
    AlbumLoader.cs \
    ClutterFlowView.cs \
    PartyModeButton.cs \
    FullscreenButton.cs \
    ArtworkLookup.cs \
    FloatingQueue.cs

RESOURCES =  \
	ClutterFlow.addin.xml

if ENABLE_CLUTTERFLOW
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
