ASSEMBLY = Banshee.Mirage
TARGET = library

SOURCES = \
	MiragePlugin.cs \
	PlaylistGeneratorSource.cs \
	SimilarityCalculator.cs \
	Config.cs

RESOURCES = \
	res/cp.png \
	res/source-mirage.png \
	res/MirageMenu.xml \
	res/GlobalUI.xml \
	Banshee.Plugins.Mirage.addin.xml

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	-r:../Mirage/Mirage.dll

include $(top_srcdir)/build/build.mk
