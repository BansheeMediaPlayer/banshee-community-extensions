ASSEMBLY = Banshee.OpenVP
LINK =  -r:System $(BANSHEE_LIBS) $(BANSHEE_NOWPLAYING_LIBS) $(TAO_OPENGL_LIBS) $(TAO_FREEGLUT_LIBS) \
	-r:$(DIR_BIN)/OpenVP.dll -r:$(DIR_BIN)/OpenVP.Core.dll

SOURCES =  \
	BansheePlayerData.cs \
	Gtk/GLWidget.cs \
	OpenVPService.cs \
	Settings.cs \
	VisualizationDisplayWidget.cs \
	VisualizationExtensionNode.cs

RESOURCES =  \
	resources/ActiveSourceUI.xml \
	resources/Banshee.OpenVP.addin.xml

if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
