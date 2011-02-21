ASSEMBLY = Banshee.Karaoke

REFS = -r:$(top_builddir)/bin/Banshee.Streamrecorder.dll

LINK = $(BANSHEE_LIBS) $(REFS)

SOURCES =  \
	Banshee.Karaoke/KaraokeConfigDialog.cs \
	Banshee.Karaoke/KaraokeService.cs

RESOURCES =  \
	Karaoke.addin.xml \
	Resources/KaraokeButton.xml \
	Resources/KaraokeMenu.xml \
	Resources/microphone.png

if ENABLE_KARAOKE
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
