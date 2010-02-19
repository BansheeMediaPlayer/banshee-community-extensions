ASSEMBLY = Banshee.Lirc
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	ActionMapper.cs \
	BansheeController.cs \
	ConfigDialog.cs \
	IController.cs \
	LircClient.cs \
	LircPlugin.cs \
	MockController.cs \
	gtk-gui/Banshee.Lirc.ConfigDialog.cs \
	gtk-gui/generated.cs

RESOURCES =  \
	Banshee.Lirc.addin.xml \
	Resources/Ui.xml

if ENABLE_LIRC
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
