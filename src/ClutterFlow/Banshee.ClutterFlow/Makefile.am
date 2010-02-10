ASSEMBLY = Banshee.ClutterFlow
LINK = $(BANSHEE_LIBS) $(BANSHEE_NOWPLAYING_LIBS) -pkg:clutter-sharp -pkg:clutter-gtk-sharp -r:$(DIR_BIN)/ClutterFlow.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-alarm.schemas.in

SOURCES =  \
    AssemblyInfo.cs \
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

include $(top_srcdir)/build/build.mk
