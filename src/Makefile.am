
EXTRA_DIST =  Banshee.Telepathy.dll.config

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -unsafe -noconfig -codepage:utf8 -warn:3 -optimize- -debug "-define:DEBUG"
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
ASSEMBLY_COMPILER_FLAGS =  -unsafe -noconfig -codepage:utf8 -warn:4 -optimize-
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
	$(BANSHEE_TELEPATHY_DLL_MDB) \
	$(LIBSOCKET_CONFIG)

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
	Banshee.Telepathy.Data/LibraryDownloadMonitor.cs \
	Banshee.Telepathy.Data/IContactSource.cs \
	Banshee.Telepathy.Data/TelepathyDownload.cs \
	Banshee.Telepathy.Data/TelepathyDownloadManager.cs \
	Banshee.Telepathy.Data/TransferManager.cs \
	Banshee.Telepathy.Data/TelepathyTransfer.cs \
	Banshee.Telepathy.Data/TelepathyUpload.cs \
	Banshee.Telepathy.Data/Transfer.cs \
	Banshee.Telepathy.Data/TransferList.cs \
	Banshee.Telepathy.Gui/ColumnCellContactStatusIndicator.cs \
	Banshee.Telepathy.Gui/ContactRequestDialog.cs \
	Banshee.Telepathy.Gui/DownloadManagerUi.cs \
	Banshee.Telepathy.Gui/ContactSourceContents.cs \
	Banshee.Telepathy.Gui/TelepathyActions.cs \
	Banshee.Telepathy.Gui/TransferManagerUi.cs \
	Banshee.Telepathy.Gui/UploadManagerUi.cs \
	Banshee.Telepathy.DBus/BaseProvider.cs \
	Banshee.Telepathy.DBus/IMetadataProvider.cs \
	Banshee.Telepathy.DBus/IMetadataProviderService.cs \
	Banshee.Telepathy.DBus/IPlaylistProvider.cs \
	Banshee.Telepathy.DBus/MetadataProvider.cs \
	Banshee.Telepathy.DBus/MetadataProviderService.cs \
	Banshee.Telepathy.DBus/PlaylistProvider.cs \
	Telepathy/Constants.cs \
	Telepathy/MissionControl.cs \
	Telepathy/Telepathy.cs \
	NDesk.DBus/Address.cs \
	NDesk.DBus/Authentication.cs \
	NDesk.DBus/Bus.cs \
	NDesk.DBus/BusObject.cs \
	NDesk.DBus/Connection.cs \
	NDesk.DBus/DBus.cs \
	NDesk.DBus/DProxy.cs \
	NDesk.DBus/ExportObject.cs \
	NDesk.DBus/GLib.cs \
	NDesk.DBus/GLib.IO.cs \
	NDesk.DBus/Introspection.cs \
	NDesk.DBus/IntrospectionSchema.cs \
	NDesk.DBus/Mapper.cs \
	NDesk.DBus/MatchRule.cs \
	NDesk.DBus/Message.cs \
	NDesk.DBus/MessageFilter.cs \
	NDesk.DBus/MessageReader.cs \
	NDesk.DBus/MessageWriter.cs \
	NDesk.DBus/PendingCall.cs \
	NDesk.DBus/Protocol.cs \
	NDesk.DBus/Server.cs \
	NDesk.DBus/ServerBus.cs \
	NDesk.DBus/Signature.cs \
	NDesk.DBus/SocketTransport.cs \
	NDesk.DBus/Transport.cs \
	NDesk.DBus/TypeDefiner.cs \
	NDesk.DBus/TypeImplementer.cs \
	NDesk.DBus/Unix.cs \
	NDesk.DBus/UnixMonoTransport.cs \
	NDesk.DBus/UnixNativeTransport.cs \
	NDesk.DBus/UnixTransport.cs \
	NDesk.DBus/Wrapper.cs 

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
	System.Web \
	System.Xml \
	System.Core

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

BANSHEE_TELEPATHY_PC = $(BUILD_DIR)/banshee.telepathy.pc
LIBSOCKET_CONFIG = $(BUILD_DIR)/Banshee.Telepathy.dll.config

$(eval $(call emit-deploy-wrapper,BANSHEE_TELEPATHY_PC,banshee.telepathy.pc))
$(eval $(call emit-deploy-wrapper,LIBSOCKET_CONFIG,Banshee.Telepathy.dll.config))

$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

