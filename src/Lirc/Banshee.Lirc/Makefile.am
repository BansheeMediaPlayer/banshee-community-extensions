ASSEMBLY = Banshee.Lirc
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	ActionMapper.cs \
	BansheeController.cs \
	ConfigDialog.cs \
	gtk-gui/Banshee.Lirc.ConfigDialog.cs \
	gtk-gui/generated.cs \
	IController.cs \
	LircClient.cs \
	LircPlugin.cs \
	MockController.cs

RESOURCES =  \
	Banshee.Lirc.addin.xml \
	Resources/Ui.xml

if ENABLE_LIRC
include $(top_srcdir)/build/build.mk
module_DATA = Banshee.Lirc.dll.config
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
