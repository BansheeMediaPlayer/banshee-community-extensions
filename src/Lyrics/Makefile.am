ASSEMBLY = Banshee.Lyrics
LINK = $(BANSHEE_LIBS) $(WEBKIT_SHARP_LIBS) $(GCONF_SHARP_20_LIBS) -r:System.Web.Services -r:System.Xml 
GMCS_FLAGS = -unsafe

SOURCES =  \
	Banshee.Lyrics/Banshee.Lyrics/LyricsManager.cs \
	Banshee.Lyrics/Banshee.Lyrics/LyricsDownloadJob.cs \
	Banshee.Lyrics/Banshee.Lyrics.Gui/ContextPage.cs \
	Banshee.Lyrics/Banshee.Lyrics.Gui/LyricsPane.cs \
	Banshee.Lyrics/Banshee.Lyrics/LyricsService.cs \
	Banshee.Lyrics/Banshee.Lyrics.Network/ProxyManager.cs \
	Banshee.Lyrics/Banshee.Lyrics.Network/HttpUtils.cs \  
	Banshee.Lyrics/Banshee.Lyrics.Sources/Lyrc.cs \
	Banshee.Lyrics/Banshee.Lyrics.Sources/AutoLyrics.cs \
	Banshee.Lyrics/Banshee.Lyrics.Sources/ILyricsSource.cs \
	Banshee.Lyrics/Banshee.Lyrics.Sources/LyricWiki.cs \
	Banshee.Lyrics/Banshee.Lyrics.Sources/Lyriki.cs \
	Banshee.Lyrics/Banshee.Lyrics.Sources/LeosLyrics.cs \
	Banshee.Lyrics/Banshee.Lyrics.Sources/LyricsWebSource.cs \
	Banshee.Lyrics/Banshee.Lyrics.Gui/LyricsWindow.cs \
	Banshee.Lyrics/Banshee.Lyrics.Gui/gtk-gui/generated.cs \
	Banshee.Lyrics/Banshee.Lyrics.Gui/gtk-gui/LyricsWindow.cs \
	Banshee.Lyrics/Banshee.Lyrics.IO/LyricsCache.cs \
	Banshee.Lyrics/Banshee.Lyrics/Utils.cs \
	Banshee.Lyrics/Banshee.Lyrics.Gui/LyricsBrowser.cs 

RESOURCES =  \
	Lyrics.addin.xml \
	Resources/LyricsMenu.xml

include $(top_srcdir)/build/build.mk
