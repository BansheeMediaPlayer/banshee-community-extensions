ASSEMBLY = Banshee.Mirage
LINK = $(BANSHEE_LIBS) -r:$(DIR_BIN)/Mirage.dll

SOURCES =  \
	AnalyzeLibraryJob.cs \
	BaseSeed.cs \
	DistanceCalculator.cs \
	MirageConfiguration.cs \
	MiragePlugin.cs \
	RandomBySimilar.cs \
	SingleSeed.cs \
	TrackAnalysis.cs

RESOURCES =  \
	Banshee.Mirage.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/cp.png \
	Resources/GlobalUI.xml \
	Resources/MirageMenu.xml \
	Resources/source-mirage.png

if ENABLE_MIRAGE
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
