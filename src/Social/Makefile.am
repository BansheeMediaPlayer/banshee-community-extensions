ASSEMBLY = Banshee.Social
LINK = $(BANSHEE_LIBS) $(LIBGWIBBERSHARP_LIBS)

SOURCES = Banshee.Social/SocialService.cs

RESOURCES = Social.addin.xml

if ENABLE_SOCIAL
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
