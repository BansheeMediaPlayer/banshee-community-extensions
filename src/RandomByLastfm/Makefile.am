ASSEMBLY = Banshee.RandomByLastfm
LINK = $(BANSHEE_LIBS) $(BANSHEE_LASTFM_LIBS)
SOURCES =  \
	Banshee.RandomByLastfm/RandomByLastfmSimilarArtists.cs \
	Banshee.RandomByLastfm/RandomByLastfmUserTopArtists.cs \
	Banshee.RandomByLastfm/WeightedRandom.cs

RESOURCES = RandomByLastfm.addin.xml

if ENABLE_RANDOMBYLASTFM
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
