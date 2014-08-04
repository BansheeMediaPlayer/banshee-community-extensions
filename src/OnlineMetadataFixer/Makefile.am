ASSEMBLY = Banshee.OnlineMetadataFixer
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.OnlineMetadataFixer/AcoustIDFingerprintJob.fs \
	Banshee.OnlineMetadataFixer/AcoustIDKeysHelper.fs \
	Banshee.OnlineMetadataFixer/AcoustIDPreferences.fs \
	Banshee.OnlineMetadataFixer/AcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/AcoustIDSender.fs \
	Banshee.OnlineMetadataFixer/AcoustIDStorage.fs \
	Banshee.OnlineMetadataFixer/AcoustIDSubmitJob.fs \
	Banshee.OnlineMetadataFixer/AllMetadataFixer.fs \
	Banshee.OnlineMetadataFixer/HelperRecords.fs \
	Banshee.OnlineMetadataFixer/InvalidMetadataSolver.fs \
	Banshee.OnlineMetadataFixer/JSonAcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/MissingAlbumSolver.fs \
	Banshee.OnlineMetadataFixer/MissingArtistTitleSolver.fs \
	Banshee.OnlineMetadataFixer/MissingFromAcoustIDSolver.fs \
	Banshee.OnlineMetadataFixer/MissingMostOfMetadataSolver.fs \
	Banshee.OnlineMetadataFixer/OnlineMetadataFixerSolver.fs

RESOURCES =  \
	OnlineMetadataFixer.addin.xml \
	Resources/AcoustIDSubmissionStatus.json \
	Resources/AcoustIDTrackInfo.json \
	Resources/MusicBrainzTracks.xml

if ENABLE_ONLINEMETADATAFIXER
include $(top_srcdir)/build/xbuild.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
