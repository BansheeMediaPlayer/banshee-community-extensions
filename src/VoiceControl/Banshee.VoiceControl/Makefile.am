ASSEMBLY = Banshee.VoiceControl
LINK = $(BANSHEE_LIBS)
TEST_ASSEMBLY = no
if ENABLE_VOICECONTROL
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
