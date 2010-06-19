ASSEMBLY = Banshee.SoundMenu
LINK = $(BANSHEE_LIBS) $(INDICATESHARP_LIBS)

SOURCES = Banshee.SoundMenu/SoundMenuService.cs

RESOURCES = SoundMenu.addin.xml

if ENABLE_SOUNDMENU
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
