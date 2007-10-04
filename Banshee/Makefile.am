SUBDIRS = res

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY = Banshee.Plugins.Mirage.dll

SOURCES = \
	ContinuousGeneratorSource.cs \
	MiragePlugin.cs \
	PlaylistGeneratorSource.cs \
	PlaylistGeneratorView.cs \
	Config.cs \
	AssemblyInfo.cs \
	PlaylistModel.cs \
	TrackView.Columns/TrackViewColumn.cs \
	TrackView.Columns/TrackNumberColumn.cs \
	TrackView.Columns/ArtistColumn.cs \
	TrackView.Columns/TitleColumn.cs \
	TrackView.Columns/AlbumColumn.cs \
	TrackView.Columns/GenreColumn.cs \
	TrackView.Columns/YearColumn.cs \
	TrackView.Columns/DurationColumn.cs \
	TrackView.Columns/PlayCountColumn.cs \
	TrackView.Columns/LastPlayedColumn.cs \
	TrackView.Columns/RatingColumn.cs \
	TrackView.Columns/UriColumn.cs \
	TrackView.Columns/DateAddedColumn.cs


RESOURCES = \
	-resource:res/cp.png \
	-resource:res/source-mirage.png \
	-resource:res/MirageMenu.xml

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	-r:Mono.Posix \
	-r:Mono.Data \
	-r:Mono.Data.SqliteClient \
	-r:System.Data \
	-r:System \
	-r:../Mirage/Mirage.dll \
	$(BANSHEE_LIBS)

all: $(ASSEMBLY)

$(ASSEMBLY) $(ASSEMBLY).mdb: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$(ASSEMBLY) $(RESOURCES) $(REFERENCES) -target:library $(BANSHEE_LIBS) $(SOURCES)

EXTRA_DIST = \
	$(SOURCES) \
	res/cp.png \
	res/source-mirage.png \
	res/MirageMenu.xml

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

mirageplugindir = $(BANSHEE_SYSTEM_PLUGINS)
mirageplugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb
