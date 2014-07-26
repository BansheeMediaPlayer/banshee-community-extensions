ASSEMBLY = Banshee.OnlineMetadataFixer
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.OnlineMetadataFixer/AcoustIDFingerprintJob.fs \
	Banshee.OnlineMetadataFixer/AcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/AcoustIDStorage.fs \
	Banshee.OnlineMetadataFixer/AllMetadataFixer.fs \
	Banshee.OnlineMetadataFixer/HelperRecords.fs \
	Banshee.OnlineMetadataFixer/InvalidMetadataSource.fs \
	Banshee.OnlineMetadataFixer/JSonAcoustIDReader.fs \
	Banshee.OnlineMetadataFixer/MissingAlbumSource.fs \
	Banshee.OnlineMetadataFixer/MissingArtistTitleSource.fs \
	Banshee.OnlineMetadataFixer/MissingFromAcoustIDSource.fs \
	Banshee.OnlineMetadataFixer/MissingMostOfMetadataSource.fs \
	Banshee.OnlineMetadataFixer/OnlineMetadataFixerSource.fs \
	Banshee.OnlineMetadataFixer/PasswordManager.fs

RESOURCES =  \
	OnlineMetadataFixer.addin.xml \
	Resources/AcoustIDTrackInfo.json \
	Resources/MusicBrainzTracks.xml

if ENABLE_ONLINEMETADATAFIXER
include $(top_srcdir)/build/xbuild.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
