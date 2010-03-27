ASSEMBLY = Banshee.AppIndicator
LINK = $(BANSHEE_LIBS) $(APPINDICATORSHARP_LIBS) $(NOTIFYSHARP_LIBS)

SOURCES = Banshee.AppIndicator/AppIndicatorService.cs

RESOURCES =  \
	AppIndicator.addin.xml \
	Resources/AppIndicatorMenu.xml

if ENABLE_APPINDICATOR
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
