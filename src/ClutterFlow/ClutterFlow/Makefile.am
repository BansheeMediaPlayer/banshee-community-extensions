ASSEMBLY = ClutterFlow
LINK = $(CLUTTER_SHARP_LIBS) -r:Mono.Cairo -pkg:gtk-sharp-2.0
GMCS_FLAGS = -unsafe

SOURCES =  \
    AssemblyInfo.cs \
    ClutterSlider.cs \
    ClutterSliderHandleButton.cs \
    ClutterButton.cs \
    ClutterSliderHandle.cs \
    ClutterArrowButton.cs \
    ClutterHelper.cs \
    ClutterFlowSlider.cs \
    ClutterFlowTimeline.cs \
    CoverCaption.cs \
    CoverManager.cs \
    FlowBehaviour.cs \
    ClutterFlowActor.cs \
    ClutterFlowFixedActor.cs \
    ActorLoader.cs \
    ClutterWidgetBar.cs \
    ClutterGenericButton.cs \
    ClutterToggleButton.cs \
    TrackCaption.cs \
    Caption.cs

if ENABLE_CLUTTERFLOW
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
