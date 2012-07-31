ASSEMBLY = Banshee.VoiceControl
LINK = $(BANSHEE_LIBS)
TEST_ASSEMBLY = no

SOURCES = libvoicecontrol.cs

RESOURCES = Banshee.VoiceControl.addin.xml 

if ENABLE_VOICECONTROL
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
