ASSEMBLY = Banshee.Telepathy
LINK = \
	$(BANSHEE_LIBS) \
	$(NOTIFY_SHARP_LIBS) \
	-r:System \
	-r:System.Core \
	-r:System.Web \
	-r:System.Xml
GMCS_FLAGS = -unsafe

SOURCES =  \
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
	Banshee.Telepathy.Data/TubeManager.cs \
	Banshee.Telepathy.Gui/ColumnCellContactStatusIndicator.cs \
	Banshee.Telepathy.Gui/ContactRequestDialog.cs \
	Banshee.Telepathy.Gui/DownloadManagerUi.cs \
	Banshee.Telepathy.Gui/ContactSourceContents.cs \
	Banshee.Telepathy.Gui/TelepathyActions.cs \
	Banshee.Telepathy.Gui/TransferManagerUi.cs \
	Banshee.Telepathy.Gui/TelepathyNotification.cs \
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
	Telepathy/Telepathy.cs

RESOURCES = \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml \
	Banshee.Telepathy.addin.xml

if ENABLE_TELEPATHY
include $(top_srcdir)/build/build.mk
EXTRA_DIST += Banshee.Telepathy.dll.config
module_DATA = Banshee.Telepathy.dll.config
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
