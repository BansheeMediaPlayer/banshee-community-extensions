ASSEMBLY = Banshee.FolderSync
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.FolderSync/FolderSyncController.cs \
	Banshee.FolderSync/FolderSyncSource.cs \
	Banshee.FolderSync/FolderSyncView.cs

RESOURCES =  \
	FolderSync.addin.xml \
	Resources/ActiveUI.xml \
	Resources/MenuUI.xml

if ENABLE_FOLDERSYNC
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
