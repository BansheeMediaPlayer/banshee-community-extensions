MCS_FLAGS = -debug /r:System.Web 
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug
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
        Banshee.Lyrics.addin.xml \
	LyricsMenu.xml

REFERENCES =  \
	System.Web \
	$(GTKHTML_SHARP_314_LIBS) \
	$(GCONF_SHARP_20_LIBS) \
	$(BANSHEE_1_THICKCLIENT_LIBS)

BUILD_REFERENCES = $(foreach ref, $(REFERENCES), $(if $(filter -pkg:%, $(ref)), $(ref), $(if $(filter -r:%, $(ref)), $(ref), -r:$(ref))))

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

plugindir = $(PLUGINDIR)
plugin_SCRIPTS = $(ASSEMBLY)

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
	$(MCS) $(MCS_FLAGS) $(CUSTOM_MCS_FLAGS) $(ASSEMBLY_COMPILER_FLAGS)  -out:$(ASSEMBLY) -target:library $(BANSHEE_LIBS) $(SOURCES_BUILD) $(RESOURCES_BUILD) $(BUILD_REFERENCES)

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
MAINTAINERCLEANFILES = Makefile.in

