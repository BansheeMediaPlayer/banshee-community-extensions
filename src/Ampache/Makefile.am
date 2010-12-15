ASSEMBLY = Banshee.Ampache
LINK = $(BANSHEE_LIBS) -r:System.Xml.Linq

SOURCES =  \
	Banshee.Ampache/AmpachePreferences.cs \
	Banshee.Ampache/AmpacheSource.cs \
	Banshee.Ampache/AmpacheSourceContents.cs \
	Banshee.Ampache/Entities/AmpacheAlbum.cs \
	Banshee.Ampache/Entities/AmpacheArtist.cs \
	Banshee.Ampache/Entities/AmpachePlaylist.cs \
	Banshee.Ampache/Entities/AmpacheSong.cs \
	Banshee.Ampache/Entities/Handshake.cs \
	Banshee.Ampache/Entities/IEntity.cs \
	Banshee.Ampache/Entities/IRatable.cs \
	Banshee.Ampache/Entities/ITagable.cs \
	Banshee.Ampache/Entities/Tag.cs \
	Banshee.Ampache/Gui/AlbumLabel.cs \
	Banshee.Ampache/Gui/AmpacheView.cs \
	Banshee.Ampache/Gui/ArtistLabel.cs \
	Banshee.Ampache/Gui/PlaylistLabel.cs \
	Banshee.Ampache/Gui/PreferenceView.cs \
	Banshee.Ampache/Gui/SongLabel.cs \
	Banshee.Ampache/IO/AlbumFactory.cs \
	Banshee.Ampache/IO/AlbumSelector.cs \
	Banshee.Ampache/IO/AmpacheSelectionFactory.cs \
	Banshee.Ampache/IO/AmpacheSelectorBase.cs \
	Banshee.Ampache/IO/ArtistFactory.cs \
	Banshee.Ampache/IO/ArtistSelector.cs \
	Banshee.Ampache/IO/Authenticate.cs \
	Banshee.Ampache/IO/FactoryBase.cs \
	Banshee.Ampache/IO/FactoryBaseRatable.cs \
	Banshee.Ampache/IO/FactoryBaseTagable.cs \
	Banshee.Ampache/IO/IAmpacheSelector.cs \
	Banshee.Ampache/IO/IEntityFactory.cs \
	Banshee.Ampache/IO/PlaylistFactory.cs \
	Banshee.Ampache/IO/PlaylistSelector.cs \
	Banshee.Ampache/IO/SongFactory.cs \
	Banshee.Ampache/IO/SongSelector.cs \
	Banshee.Ampache/Logic/AmpacheAlbumExtensions.cs \
	Banshee.Ampache/Logic/AmpacheSongExtensions.cs \
	Banshee.Ampache/Logic/AmpacheViewModel.cs \
	Banshee.Ampache/Logic/PlayQueue.cs \
	gtk-gui/Banshee.Ampache.PreferenceView.cs \
	gtk-gui/generated.cs

RESOURCES =  \
	Ampache.addin.xml \
	gtk-gui/gui.stetic \
	Resources/ampache.png

if ENABLE_AMPACHE
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
