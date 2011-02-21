ASSEMBLY = Banshee.Jamendo
LINK = $(BANSHEE_LIBS) $(BANSHEE_WEBBROWSER_LIBS) -r:ICSharpCode.SharpZipLib

SOURCES =  \
	Banshee.Jamendo/JamendoDownloadManager.cs \
	Banshee.Jamendo/JamendoSource.cs \
	Banshee.Jamendo/JamendoView.cs \
	Banshee.Jamendo/JamendoWebBrowserShell.cs

RESOURCES =  \
	Jamendo.addin.xml \
	ThemeIcons/22x22/categories/jamendo.png

if ENABLE_JAMENDO
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
