ASSEMBLY = Banshee.Mirage
LINK = $(BANSHEE_LIBS) -r:$(DIR_BIN)/Mirage.dll

SOURCES =  \
	AnalyzeLibraryJob.cs \
	MirageConfiguration.cs \
	MiragePlugin.cs \
	PlaylistGeneratorSource.cs \
	RandomBySimilar.cs \
	SimilarityCalculator.cs

RESOURCES =  \
	Banshee.Mirage.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/cp.png \
	Resources/GlobalUI.xml \
	Resources/MirageMenu.xml \
	Resources/source-mirage.png

include $(top_srcdir)/build/build.mk
