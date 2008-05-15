MCS_FLAGS = -debug /r:System.Web 
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -resource:./LyricsMenu.xml,LyricsMenu.xml  "-define:DEBUG"
CUSTOM_MCS_FLAGS = $(MCS_FLAGS) \
	-unsafe
ASSEMBLY = Banshee.Lyrics.dll
SOURCES = \
 	gtk-gui/generated.cs \
        AutoLyrics.cs \
        LyricBaseSource.cs \
        CacheOperation.cs \
        Constants.cs \
        Lyrc.cs \
        LyricsManager.cs \
        LyricsPlugin.cs \
        Lyriki.cs \
        Lyriki2.cs \
        ProxyManager.cs \
        LyricsWindow.cs \
        LyricsHeader.cs \
        gtk-gui/Banshee.Plugins.Lyrics.LyricsBrowser.cs \
        gtk-gui/Banshee.Plugins.Lyrics.LyricsHeader.cs \
        gtk-gui/Banshee.Plugins.Lyrics.LyricsWindow.cs \
        LyricsCache.cs \
        LyricsBrowser.cs \
        LyricsPluginCommon.cs \
        LyricsEvent.cs

RESOURCES = \
	gtk-gui/gui.stetic \
        gtk-gui/objects.xml \
        Banshee.Lyrics.addin.xml

RESOURCES_BUILD = $(foreach resource,$(RESOURCES),$(addprefix -resource:,$(resource)))

EXTRAS = \
	bin/Debug/BansheeLyricsPlugin-mono.dll.mdb \
	bin/Debug/Lyrics.dll.mdb \
	AssemblyInfo.cs.in \
	BansheeLyricsPlugin-mono.userprefs \
	BansheeLyricsPlugin-mono.usertasks \
	bin/Debug/Banshee.Lyrics.dll.mdb 

REFERENCES =  \
	System.Web \
	$(GTKHTML_SHARP) \
	$(GTKHTML_SHARP_LIBS) \
	$(GCONF_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(BANSHEE_1_CORE_LIBS) \
	$(BANSHEE_1_SERVICES_LIBS) \
	$(BANSHEE_1_THICKCLIENT_LIBS)

BUILD_REFERENCES = $(foreach ref, $(REFERENCES), $(if $(filter -pkg:%, $(ref)), $(ref), $(if $(filter -r:%, $(ref)), $(ref), -r:$(ref))))


plugindir = $(PLUGINDIR)
plugin_SCRIPTS = $(ASSEMBLY)

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(RESOURCES)
	$(MCS) $(MCS_FLAGS) $(CUSTOM_MCS_FLAGS) $(ASSEMBLY_COMPILER_FLAGS)  -out:$(ASSEMBLY) -target:library $(BANSHEE_LIBS) $(SOURCES) $(RESOURCES_BUILD) $(BUILD_REFERENCES);

EXTRA_DIST = $(SOURCES)
CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = Makefile.in

