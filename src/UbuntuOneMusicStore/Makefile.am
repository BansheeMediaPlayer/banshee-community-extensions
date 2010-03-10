ASSEMBLY = Banshee.UbuntuOneMusicStore
LINK = $(BANSHEE_LIBS) $(UBUNTUONESHARP_LIBS)

SOURCES = Banshee.UbuntuOneMusicStore/UbuntuOneMusicStoreSource.cs

RESOURCES =  \
	Resources/ubuntuone.png \
	UbuntuOneMusicStore.addin.xml

if ENABLE_U1MS
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
