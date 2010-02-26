ASSEMBLY = Banshee.CoverWallpaper
LINK = $(BANSHEE_LIBS) $(GCONFSHARP_LIBS)

SOURCES = Banshee.CoverWallpaper/CoverWallpaperService.cs

RESOURCES = Banshee.CoverWallpaper.addin.xml

if ENABLE_COVERWALLPAPER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
