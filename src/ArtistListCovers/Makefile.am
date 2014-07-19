ASSEMBLY = Banshee.ArtistListCovers
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.ArtistListCovers/ColumnCellArtistCover.cs \
	Banshee.ArtistListCovers/ColumnCellArtistCoverLarge.cs \
	Banshee.ArtistListCovers/ColumnCellArtistCoverSmall.cs

RESOURCES = ArtistListCovers.addin.xml

if ENABLE_ARTISTLISTCOVERS
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
