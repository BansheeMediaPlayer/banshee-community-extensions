ASSEMBLY = OpenVP
LINK =  -r:System $(TAO_OPENGL_LIBS) $(TAO_FREEGLUT_LIBS) $(TAO_SDL_LIBS)

SOURCES =  \
	Color.cs \
	Effect.cs \
	IBeatDetector.cs \
	IController.cs \
	IPreset.cs \
	IRenderer.cs \
	KeyboardEvent.cs \
	LinearPreset.cs \
	Metadata/AuthorAttribute.cs \
	Metadata/BrowsableAttribute.cs \
	Metadata/CategoryAttribute.cs \
	Metadata/DefaultValueAttribute.cs \
	Metadata/DescriptionAttribute.cs \
	Metadata/DisplayNameAttribute.cs \
	Metadata/FollowsAttribute.cs \
	Metadata/RangeAttribute.cs \
	NullPlayerData.cs \
	PlayerData.cs \
	Registry.cs \
	SDLController.cs \
	TimedPresetBase.cs \
	UDPPlayerData.cs

RESOURCES =  

if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
