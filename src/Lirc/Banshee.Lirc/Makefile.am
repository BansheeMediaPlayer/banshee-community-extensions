ASSEMBLY = Banshee.Lirc
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	ActionMapper.cs \
	BansheeController.cs \
	IController.cs \
	LircClient.cs \
	LircPlugin.cs

RESOURCES = Banshee.Lirc.addin.xml

if ENABLE_LIRC
include $(top_srcdir)/build/build.mk
EXTRA_DIST += Banshee.Lirc.dll.config
module_DATA = Banshee.Lirc.dll.config
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
