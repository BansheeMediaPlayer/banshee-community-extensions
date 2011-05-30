ASSEMBLY = ClutterFlow
LINK = $(CLUTTER_SHARP_LIBS) -r:Mono.Cairo -pkg:gtk-sharp-2.0
GMCS_FLAGS = -unsafe

SOURCES =  \
	AlphabetBar.cs \
	AlphabetButton.cs \
	Caption.cs \
	ClutterArrowButton.cs \
	ClutterButton.cs \
	ClutterFlowActor.cs \
	ClutterFlowBaseActor.cs \
	ClutterFlowFixedActor.cs \
	ClutterFlowSlider.cs \
	ClutterFlowTimeline.cs \
	ClutterGenericButton.cs \
	ClutterHelper.cs \
	ClutterSlider.cs \
	ClutterSliderHandle.cs \
	ClutterSliderHandleButton.cs \
	ClutterToggleButton.cs \
	ClutterWidgetBar.cs \
	CoverCaption.cs \
	CoverManager.cs \
	FlowBehaviour.cs \
	IActorLoader.cs \
	TextureHolder.cs \
	TrackCaption.cs

if ENABLE_CLUTTERFLOW
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
