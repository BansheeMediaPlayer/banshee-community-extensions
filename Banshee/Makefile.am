SUBDIRS = res

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY = Banshee.Plugins.Mirage.dll

SOURCES = \
	ContinuousGeneratorSource.cs \
	MiragePlugin.cs \
	PlaylistColumns.cs \
	PlaylistGeneratorSource.cs \
	PlaylistGeneratorView.cs \
	AssemblyInfo.cs

RESOURCES = \
	-resource:res/cp.png \
	-resource:res/mirage.png \
	-resource:res/source-localqueue.png

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
	$(SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

mirageplugindir = $(BANSHEE_SYSTEM_PLUGINS)
mirageplugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb
