ASSEMBLY = Banshee.LCD
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.LCD/ConfigurationDialog.cs \
	Banshee.LCD/ConfigurationSchema.cs \
	Banshee.LCD/LCD.cs \
	Banshee.LCD/LCDClient.cs \
	Banshee.LCD/LCDParser.cs \
	Banshee.LCD/LCDScreen.cs \
	Banshee.LCD/LCDService.cs \
	Banshee.LCD/LCDWidget.cs

RESOURCES =  \
	LCD.addin.xml \
	Resources/LCDMenu.xml

if ENABLE_LCD
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
