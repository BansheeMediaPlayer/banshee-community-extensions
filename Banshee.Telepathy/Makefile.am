
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:3 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Banshee.Telepathy.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

BANSHEE_TELEPATHY_DLL_MDB_SOURCE=bin/Debug/Banshee.Telepathy.dll.mdb
BANSHEE_TELEPATHY_DLL_MDB=$(BUILD_DIR)/Banshee.Telepathy.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Banshee.Telepathy.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

BANSHEE_TELEPATHY_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(BANSHEE_TELEPATHY_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(BANSHEE_TELEPATHY_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	Banshee.Telepathy.API/Account.cs \
	Banshee.Telepathy.API/Announcer.cs \
	Banshee.Telepathy.API/Avatar.cs \
	Banshee.Telepathy.API/Connection.cs \
	Banshee.Telepathy.API/ConnectionLocator.cs \
	Banshee.Telepathy.API/Contact.cs \
	Banshee.Telepathy.API/DispatchManager.cs \
	Banshee.Telepathy.API/Roster.cs \
	Banshee.Telepathy.API/Channels/Channel.cs \
	Banshee.Telepathy.API/Channels/RequestedChannel.cs \
	Banshee.Telepathy.API/Channels/ContactListChannel.cs \
	Banshee.Telepathy.API/Channels/DBusTubeChannel.cs \
	Banshee.Telepathy.API/Channels/FileTransferChannel.cs \
	Banshee.Telepathy.API/Channels/StreamTubeChannel.cs \
	Banshee.Telepathy.API/Channels/IChannel.cs \
	Banshee.Telepathy.API/Channels/ITube.cs \
	Banshee.Telepathy.API/Data/ChannelInfo.cs \
	Banshee.Telepathy.API/Data/ChannelInfoCollection.cs \
	Banshee.Telepathy.API/Data/DBusTubeChannelInfo.cs \
	Banshee.Telepathy.API/Data/IServiceProvidingChannel.cs\
	Banshee.Telepathy.API/Data/StreamTubeChannelInfo.cs \
	Banshee.Telepathy.API/Data/FileTransferChannelInfo.cs \
	Banshee.Telepathy.API/DBus/DBusUtility.cs \
	Banshee.Telepathy.API/DBus/DBusConnection.cs \
	Banshee.Telepathy.API/DBus/EmpathyHandler.cs \
	Banshee.Telepathy.API/Dispatchables/Activity.cs \
	Banshee.Telepathy.API/Dispatchables/DBusActivity.cs \
	Banshee.Telepathy.API/Dispatchables/Dispatchable.cs \
	Banshee.Telepathy.API/Dispatchables/DispatchableQueue.cs \
	Banshee.Telepathy.API/Dispatchables/FileTransfer.cs \
	Banshee.Telepathy.API/Dispatchables/IncomingFileTransfer.cs \
	Banshee.Telepathy.API/Dispatchables/OutgoingFileTransfer.cs \
	Banshee.Telepathy.API/Dispatchables/StreamActivity.cs \
	Banshee.Telepathy.API/Dispatchables/StreamActivityListener.cs \
	Banshee.Telepathy.API/Dispatchers/ChannelHandler.cs \
	Banshee.Telepathy.API/Dispatchers/DBusActivityDispatcher.cs \
	Banshee.Telepathy.API/Dispatchers/StreamActivityDispatcher.cs \
	Banshee.Telepathy.API/Dispatchers/Dispatcher.cs \
	Banshee.Telepathy.API/Dispatchers/FileTransferDispatcher.cs \
	Banshee.Telepathy/TelepathyService.cs \
	Banshee.Telepathy.Net/BaseWebServer.cs \
	Banshee.Telepathy.Net/StreamingHTTPProxyServer.cs \
	Banshee.Telepathy.Net/StreamingServer.cs \
	Banshee.Telepathy.Data/ContactContainerSource.cs \
	Banshee.Telepathy.Data/ContactSource.cs \
	Banshee.Telepathy.Data/ContactTrackInfo.cs \
	Banshee.Telepathy.Data/ContactPlaylistSource.cs \
	Banshee.Telepathy.Data/DownloadMonitor.cs \
	Banshee.Telepathy.Data/IContactSource.cs \
	Banshee.Telepathy.Gui/ColumnCellContactStatusIndicator.cs \
	Banshee.Telepathy.Gui/ContactRequestDialog.cs \
	Banshee.Telepathy.Gui/DownloadManager.cs \
	Banshee.Telepathy.Gui/ContactSourceContents.cs \
	Banshee.Telepathy.Gui/TelepathyActions.cs \
	Banshee.Telepathy.Gui/TransferManager.cs \
	Banshee.Telepathy.Gui/UploadManager.cs \
	Banshee.Telepathy.DBus/BaseProvider.cs \
	Banshee.Telepathy.DBus/IMetadataProvider.cs \
	Banshee.Telepathy.DBus/IMetadataProviderService.cs \
	Banshee.Telepathy.DBus/IPlaylistProvider.cs \
	Banshee.Telepathy.DBus/MetadataProvider.cs \
	Banshee.Telepathy.DBus/MetadataProviderService.cs \
	Banshee.Telepathy.DBus/PlaylistProvider.cs \
	Telepathy/Constants.cs \
	Telepathy/MissionControl.cs \
	Telepathy/Telepathy.cs


DATA_FILES = 

RESOURCES = \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml \
	Banshee.Telepathy.addin.xml

EXTRAS =  \
	banshee.telepathy.pc.in 

REFERENCES =  \
	$(BANSHEE_1_CORE_LIBS) \
	$(BANSHEE_1_SERVICES_LIBS) \
	$(BANSHEE_1_THICKCLIENT_LIBS) \
	$(GTK_SHARP_20_LIBS) \
	$(NDESK_DBUS_10_LIBS) \
	System.Web

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

BANSHEE_TELEPATHY_PC = $(BUILD_DIR)/banshee.telepathy.pc

$(eval $(call emit-deploy-wrapper,BANSHEE_TELEPATHY_PC,banshee.telepathy.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

