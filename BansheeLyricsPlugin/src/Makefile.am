ASSEMBLY = Banshee.Lyrics.dll
TARGET = library
MCS_FLAGS = -debug /r:System.Web 
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug
CUSTOM_MCS_FLAGS = $(MCS_FLAGS) \
	-unsafe
SOURCES = \
        Banshee.Lyrics/LyricsManager.cs \
        Banshee.Lyrics.Gui/ContextPage.cs \
	Banshee.Lyrics.Gui/LyricPane.cs \
        Banshee.Lyrics/LyricsService.cs \
        Banshee.Lyrics.Network/ProxyManager.cs \
	Banshee.Lyrics.Network/HttpUtils.cs \  
      	Banshee.Lyrics.Sources/Lyrc.cs \
        Banshee.Lyrics.Sources/AutoLyrics.cs \
        Banshee.Lyrics.Sources/ILyricSource.cs \
        Banshee.Lyrics.Sources/LyricWiki.cs \
        Banshee.Lyrics.Sources/Lyriki.cs \
        Banshee.Lyrics.Sources/LeosLyrics.cs \
        Banshee.Lyrics.Sources/LyricWebSource.cs \
	Banshee.Lyrics.Gui/LyricsWindow.cs \
        Banshee.Lyrics.Gui/gtk-gui/LyricsBrowser.cs \
        Banshee.Lyrics.Gui/gtk-gui/generated.cs \
        Banshee.Lyrics.Gui/gtk-gui/LyricsWindow.cs \
        Banshee.Lyrics.IO/LyricsCache.cs \
        Banshee.Lyrics.Gui/LyricsBrowser.cs 
RESOURCES = \
        Banshee.Lyrics.addin.xml \
	Resources/LyricsMenu.xml

REFERENCES =  \
	System.Web \
	System.Web.Services \ 
	System.Xml \ 
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

