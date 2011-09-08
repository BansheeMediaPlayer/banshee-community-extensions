ASSEMBLY = Banshee.DuplicateSongDetector
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.DuplicateSongDetector/DuplicateSongDetectorAction.cs \
	Banshee.DuplicateSongDetector/DuplicateSongDetectorSource.cs \
	Banshee.DuplicateSongDetector/SongDuplicateView.cs

RESOURCES =  \
	DuplicateSongDetector.addin.xml \
	Resources/ActiveUI.xml \
	Resources/GlobalUI.xml

if ENABLE_DUPLICATESONGDETECTOR
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
