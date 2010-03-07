ASSEMBLY = Banshee.Magnatune
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.Magnatune/Configuration.cs \
	Banshee.Magnatune/gtk-gui/generated.cs \
	Banshee.Magnatune/gtk-gui/Magnatune.Configuration.cs \
	Banshee.Magnatune/RadioSource.cs \
	Banshee.Magnatune/RadioSourceContents.cs \
	Banshee.Magnatune/Widgets.cs

RESOURCES =  \
	Banshee.Magnatune/gtk-gui/gui.stetic \
	Magnatune.addin.xml \
	Resources/genres.xml \
	Resources/logo_color_large.gif \
	Resources/MagnatuneMenu.xml \
	Resources/simple_icon.png

if ENABLE_MAGNATUNE
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
