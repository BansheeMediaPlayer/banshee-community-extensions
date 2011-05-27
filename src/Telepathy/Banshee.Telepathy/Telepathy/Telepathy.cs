/*
 *   Copyright (C) 2009 Neil Loknath <neil.loknath@gmail.com>
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published
 *   by the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU LesserGeneral Public License as published
 *   by the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 */

// Generated from the Telepathy spec
#define USE_DBUS_PROPERTIES

using System;
using System.Collections.Generic;
using DBus;

namespace Telepathy
{

    [Interface ("org.freedesktop.Telepathy.ConnectionManager")]
    public interface IConnectionManager
    {

        // Method
        ParamSpec[] GetParameters (string @protocol);
        // Method
        string[] ListProtocols ();
        // Method
        void RequestConnection (string @protocol, IDictionary<string,object> @parameters, out string @bus_name, out ObjectPath @object_path);
#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

        event NewConnectionHandler NewConnection;

    }

    public struct ParamSpec
    {
        public string Name;
        public ConnMgrParamFlags Flags;
        public string Signature;
        public object DefaultValue;
    }

    [Flags]
    public enum ConnMgrParamFlags : uint
    {
        None = 0,
        Required = 1, Register = 2, HasDefault = 4, Secret = 8, DBusProperty = 16,
    }

    public delegate void NewConnectionHandler (string @bus_name, ObjectPath @object_path, string @protocol);
    [Interface ("org.freedesktop.Telepathy.Connection")]
    public interface IConnection //: IRequests, IContacts
    {

        // Method
        void Connect ();
        // Method
        void Disconnect ();
        // Method
        string[] GetInterfaces ();
        // Method
        string GetProtocol ();
        // Method
        uint GetSelfHandle ();
        // Method
        ConnectionStatus GetStatus ();
        // Method
        void HoldHandles (HandleType @handle_type, uint[] @handles);
        // Method
        string[] InspectHandles (HandleType @handle_type, uint[] @handles);
        // Method
        ChannelInfo[] ListChannels ();
        // Method
        void ReleaseHandles (HandleType @handle_type, uint[] @handles);
        // Method
        ObjectPath RequestChannel (string @type, HandleType @handle_type, uint @handle, bool @suppress_handler);
        // Method
        uint[] RequestHandles (HandleType @handle_type, string[] @names);
#if USE_DBUS_PROPERTIES
        // Property
        uint SelfHandle { get; }
#endif

        event SelfHandleChangedHandler SelfHandleChanged;

        event NewChannelHandler NewChannel;

        event ConnectionErrorHandler ConnectionError;

        event StatusChangedHandler StatusChanged;

    }

    public struct ChannelInfo
    {
        public ObjectPath Channel;
        public string ChannelType;
        public HandleType HandleType;
        public uint Handle;
    }

    public enum HandleType : uint
    {
        None = 0, Contact = 1, Room = 2, List = 3, Group = 4,
    }

    public enum ConnectionStatus : uint
    {
        Connected = 0, Connecting = 1, Disconnected = 2,
    }

    public enum ConnectionStatusReason : uint
    {
        NoneSpecified = 0, Requested = 1, NetworkError = 2, AuthenticationFailed = 3, EncryptionError = 4, NameInUse = 5, CertNotProvided = 6, CertUntrusted = 7, CertExpired = 8, CertNotActivated = 9, CertHostnameMismatch = 10, CertFingerprintMismatch = 11, CertSelfSigned = 12, CertOtherError = 13,
    }

    public delegate void SelfHandleChangedHandler (uint @self_handle);
    public delegate void NewChannelHandler (ObjectPath @object_path, string @channel_type, HandleType @handle_type, uint @handle, bool @suppress_handler);
    public delegate void ConnectionErrorHandler (string @error, IDictionary<string,object> @details);
    public delegate void StatusChangedHandler (ConnectionStatus @status, ConnectionStatusReason @reason);
    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Aliasing")]
    public interface IAliasing : IConnection
    {

        // Method
        ConnectionAliasFlags GetAliasFlags ();
        // Method
        string[] RequestAliases (uint[] @contacts);
        // Method
        IDictionary<uint,string> GetAliases (uint[] @contacts);
        // Method
        void SetAliases (IDictionary<uint,string> @aliases);
        event AliasesChangedHandler AliasesChanged;

    }

    public struct AliasPair
    {
        public uint Handle;
        public string Alias;
    }

    [Flags]
    public enum ConnectionAliasFlags : uint
    {
        None = 0,
        UserSet = 1,
    }

    public delegate void AliasesChangedHandler (AliasPair[] @aliases);
    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Avatars")]
    public interface IAvatars : IConnection
    {

        // Method
        void GetAvatarRequirements (out string[] @mime_types, out short @min_width, out short @min_height, out short @max_width, out short @max_height, out uint @max_bytes);
        // Method
        string[] GetAvatarTokens (uint[] @contacts);
        // Method
        IDictionary<uint,string> GetKnownAvatarTokens (uint[] @contacts);
        // Method
        void RequestAvatar (uint @contact, out byte[] @data, out string @mime_type);
        // Method
        void RequestAvatars (uint[] @contacts);
        // Method
        string SetAvatar (byte[] @avatar, string @mime_type);
        // Method
        void ClearAvatar ();
#if USE_DBUS_PROPERTIES
        // Property
        string[] SupportedAvatarMIMETypes { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint MinimumAvatarHeight { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint MinimumAvatarWidth { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint RecommendedAvatarHeight { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint RecommendedAvatarWidth { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint MaximumAvatarHeight { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint MaximumAvatarWidth { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint MaximumAvatarBytes { get; }
#endif

        event AvatarUpdatedHandler AvatarUpdated;

        event AvatarRetrievedHandler AvatarRetrieved;

    }

    public delegate void AvatarUpdatedHandler (uint @contact, string @new_avatar_token);
    public delegate void AvatarRetrievedHandler (uint @contact, string @token, byte[] @avatar, string @type);
    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Capabilities")]
    public interface ICapabilities : IConnection
    {

        // Method
        CapabilityPair[] AdvertiseCapabilities (CapabilityPair[] @add, string[] @remove);
        // Method
        ContactCapability[] GetCapabilities (uint[] @handles);
        event CapabilitiesChangedHandler CapabilitiesChanged;

    }

    public struct CapabilityPair
    {
        public string ChannelType;
        public uint TypeSpecificFlags;
    }

    public struct ContactCapability
    {
        public uint Handle;
        public string ChannelType;
        public ConnectionCapabilityFlags GenericFlags;
        public uint TypeSpecificFlags;
    }

    public struct CapabilityChange
    {
        public uint Handle;
        public string ChannelType;
        public ConnectionCapabilityFlags OldGenericFlags;
        public ConnectionCapabilityFlags NewGenericFlags;
        public uint OldTypeSpecificFlags;
        public uint NewTypeSpecificFlags;
    }

    [Flags]
    public enum ConnectionCapabilityFlags : uint
    {
        None = 0,
        Create = 1, Invite = 2,
    }

    public delegate void CapabilitiesChangedHandler (CapabilityChange[] @caps);


    [Interface ("org.freedesktop.Telepathy.Connection.Interface.ContactCapabilities")]
    public interface IContactCapabilities : IConnection
    {

        // Method
        void UpdateCapabilities (HandlerCapabilities[] @caps);
        // Method
        IDictionary<uint,RequestableChannelClass[]> GetContactCapabilities (uint[] @handles);
        event ContactCapabilitiesChangedHandler ContactCapabilitiesChanged;

    }

    public struct HandlerCapabilities
    {
        public string WellKnownName;
        public IDictionary<string, object>[] ChannelClasses;
        public string[] Capabilities;
    }

    public delegate void ContactCapabilitiesChangedHandler (IDictionary<uint,RequestableChannelClass[]> @caps);

    namespace Draft
    {

        [Interface ("org.freedesktop.Telepathy.Connection.Interface.ContactInfo.DRAFT")]
        public interface IContactInfo : IConnection
        {

            // Method
            IDictionary<uint,ContactInfoField[]> GetContactInfo (uint[] @contacts);
            // Method
            ContactInfoField[] RequestContactInfo (uint @contact);
            // Method
            void SetContactInfo (ContactInfoField[] @contactinfo);
    #if USE_DBUS_PROPERTIES
            // Property
            ContactInfoFlag ContactInfoFlags { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            FieldSpec[] SupportedFields { get; }
    #endif

            event ContactInfoChangedHandler ContactInfoChanged;

        }

        public struct ContactInfoField
        {
            public string FieldName;
            public string[] Parameters;
            public string[] FieldValue;
        }

        public struct FieldSpec
        {
            public string Name;
            public string[] Parameters;
            public ContactInfoFieldFlags Flags;
            public uint Max;
        }

        public enum ContactInfoFlag : uint
        {
            CanSet = 1, Push = 2,
        }

        [Flags]
        public enum ContactInfoFieldFlags : uint
        {
            None = 0,
            ParametersMandatory = 1,
        }

        public delegate void ContactInfoChangedHandler (uint @contact, ContactInfoField[] @contactinfo);
    }

    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Contacts")]
    public interface IContacts : IConnection
    {

        // Method
        IDictionary<uint,IDictionary<string,object>> GetContactAttributes (uint[] @handles, string[] @interfaces, bool @hold);
#if USE_DBUS_PROPERTIES
        // Property
        string[] ContactAttributeInterfaces { get; }
#endif

    }

    namespace Draft
    {

        [Interface ("org.freedesktop.Telepathy.Connection.Interface.Location.DRAFT")]
        public interface ILocation : IConnection
        {

            // Method
            IDictionary<uint,IDictionary<string,object>> GetLocations (uint[] @contacts);
            // Method
            IDictionary<string,object> RequestLocation (uint @contact);
            // Method
            void SetLocation (IDictionary<string,object> @location);
    #if USE_DBUS_PROPERTIES
            // Property
            RichPresenceAccessControlType[] LocationAccessControlTypes { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            RichPresenceAccessControl LocationAccessControl { get; set; }
    #endif

            event LocationUpdatedHandler LocationUpdated;

        }

        public enum LocationAccuracyLevel : int
        {
            None = 0, Country = 1, Region = 2, Locality = 3, PostalCode = 4, Street = 5, Detailed = 6,
        }

        public delegate void LocationUpdatedHandler (uint @contact, IDictionary<string,object> @location);
    }

    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Presence")]
    public interface IPresence : IConnection, ISimplePresence
    {

        // Method
        void AddStatus (string @status, IDictionary<string,object> @parameters);
        // Method
        void ClearStatus ();
        // Method
        IDictionary<uint,LastActivityAndStatuses> GetPresence (uint[] @contacts);
        // Method
        IDictionary<string,StatusSpec> GetStatuses ();
        // Method
        void RemoveStatus (string @status);
        // Method
        void RequestPresence (uint[] @contacts);
        // Method
        void SetLastActivityTime (uint @time);
        // Method
        void SetStatus (IDictionary<string,IDictionary<string,object>> @statuses);
        event PresenceUpdateHandler PresenceUpdate;

    }

    public struct LastActivityAndStatuses
    {
        public uint LastActivity;
        public IDictionary<string,IDictionary<string,object>> Statuses;
    }

    public struct StatusSpec
    {
        public ConnectionPresenceType Type;
        public bool MaySetOnSelf;
        public bool Exclusive;
        public IDictionary<string,string> ParameterTypes;
    }

    public delegate void PresenceUpdateHandler (IDictionary<uint,LastActivityAndStatuses> @presence);
    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Renaming")]
    public interface IRenaming : IConnection
    {

        // Method
        void RequestRename (string @name);
        event RenamedHandler Renamed;

    }

    public delegate void RenamedHandler (uint @original, uint @new);
    [Interface ("org.freedesktop.Telepathy.Connection.Interface.Requests")]
    public interface IRequests : IConnection
    {

        // Method
        void CreateChannel (IDictionary<string,object> @request, out ObjectPath @channel, out IDictionary<string,object> @properties);
        // Method
        void EnsureChannel (IDictionary<string,object> @request, out bool @yours, out ObjectPath @channel, out IDictionary<string,object> @properties);
#if USE_DBUS_PROPERTIES
        // Property
        ChannelDetails[] Channels { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        RequestableChannelClass[] RequestableChannelClasses { get; }
#endif

        event NewChannelsHandler NewChannels;

        event ChannelClosedHandler ChannelClosed;

    }

    public struct ChannelDetails
    {
        public ObjectPath Channel;
        public IDictionary<string,object> Properties;
    }

    public struct RequestableChannelClass
    {
        public IDictionary<string,object> FixedProperties;
        public string[] AllowedProperties;
    }

    public delegate void NewChannelsHandler (ChannelDetails[] @channels);
    public delegate void ChannelClosedHandler (ObjectPath @removed);
    [Interface ("org.freedesktop.Telepathy.Connection.Interface.SimplePresence")]
    public interface ISimplePresence : IConnection
    {

        // Method
        void SetPresence (string @status, string @status_message);
        // Method
        IDictionary<uint,SimplePresence> GetPresences (uint[] @contacts);
#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<string,SimpleStatusSpec> Statuses { get; }
#endif

        event PresencesChangedHandler PresencesChanged;

    }

    public struct SimplePresence
    {
        public ConnectionPresenceType Type;
        public string Status;
        public string StatusMessage;
    }

    public struct SimpleStatusSpec
    {
        public ConnectionPresenceType Type;
        public bool MaySetOnSelf;
        public bool CanHaveMessage;
    }

    public struct RichPresenceAccessControl
    {
        public RichPresenceAccessControlType Type;
        public object Detail;
    }

    public enum ConnectionPresenceType : uint
    {
        Unset = 0, Offline = 1, Available = 2, Away = 3, ExtendedAway = 4, Hidden = 5, Busy = 6, Unknown = 7, Error = 8,
    }

    public enum RichPresenceAccessControlType : uint
    {
        Whitelist = 0, PublishList = 1, Group = 2, Open = 3,
    }

    public delegate void PresencesChangedHandler (IDictionary<uint,SimplePresence> @presence);


    namespace Draft
    {

        [Interface ("org.freedesktop.Telepathy.ChannelBundle.DRAFT")]
        public interface IChannelBundle
        {

    #if USE_DBUS_PROPERTIES
            // Property
            string[] Interfaces { get; }
    #endif

        }

    }

    [Interface ("org.freedesktop.Telepathy.Channel")]
    public interface IChannel
    {

        // Method
        void Close ();
        // Method
        string GetChannelType ();
        // Method
        void GetHandle (out HandleType @target_handle_type, out uint @target_handle);
        // Method
        string[] GetInterfaces ();
#if USE_DBUS_PROPERTIES
        // Property
        string ChannelType { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint TargetHandle { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string TargetID { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        HandleType TargetHandleType { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool Requested { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint InitiatorHandle { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string InitiatorID { get; }
#endif

        event ClosedHandler Closed;

    }

    public delegate void ClosedHandler ();
    [Interface ("org.freedesktop.Telepathy.Channel.FUTURE")]
    public interface IChannelFuture
    {

#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath Bundle { get; }
#endif

    }

    [Interface ("org.freedesktop.Telepathy.Channel.Type.ContactList")]
    public interface IContactList : IChannel, IGroup
    {

    }

    [Interface ("org.freedesktop.Telepathy.Channel.Type.StreamedMedia")]
    public interface IStreamedMedia : IChannel, IGroup
    {

        // Method
        MediaStreamInfo[] ListStreams ();
        // Method
        void RemoveStreams (uint[] @streams);
        // Method
        void RequestStreamDirection (uint @stream_id, MediaStreamDirection @stream_direction);
        // Method
        MediaStreamInfo[] RequestStreams (uint @contact_handle, MediaStreamType[] @types);
        event StreamAddedHandler StreamAdded;

        event StreamDirectionChangedHandler StreamDirectionChanged;

        event StreamErrorHandler StreamError;

        event StreamRemovedHandler StreamRemoved;

        event StreamStateChangedHandler StreamStateChanged;

    }

    public struct MediaStreamInfo
    {
        public uint Identifier;
        public uint Contact;
        public MediaStreamType Type;
        public MediaStreamState State;
        public MediaStreamDirection Direction;
        public MediaStreamPendingSend PendingSendFlags;
    }

    public enum MediaStreamType : uint
    {
        Audio = 0, Video = 1,
    }

    public enum MediaStreamState : uint
    {
        Disconnected = 0, Connecting = 1, Connected = 2,
    }

    public enum MediaStreamDirection : uint
    {
        None = 0, Send = 1, Receive = 2, Bidirectional = 3,
    }

    [Flags]
    public enum MediaStreamPendingSend : uint
    {
        None = 0,
        LocalSend = 1, RemoteSend = 2,
    }

    [Flags]
    public enum ChannelMediaCapabilities : uint
    {
        None = 0,
        Audio = 1, Video = 2, NATTraversalSTUN = 4, NATTraversalGTalkP2P = 8, NATTraversalICEUDP = 16,
    }

    public delegate void StreamAddedHandler (uint @stream_id, uint @contact_handle, MediaStreamType @stream_type);
    public delegate void StreamDirectionChangedHandler (uint @stream_id, MediaStreamDirection @stream_direction, MediaStreamPendingSend @pending_flags);
    public delegate void StreamErrorHandler (uint @stream_id, MediaStreamError @error_code, string @message);
    public delegate void StreamRemovedHandler (uint @stream_id);
    public delegate void StreamStateChangedHandler (uint @stream_id, MediaStreamState @stream_state);
    [Interface ("org.freedesktop.Telepathy.Channel.Type.StreamedMedia.FUTURE")]
    public interface IStreamedMediaFuture : IChannel, IStreamedMedia
    {

#if USE_DBUS_PROPERTIES
        // Property
        bool InitialAudio { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool InitialVideo { get; }
#endif

    }

    [Interface ("org.freedesktop.Telepathy.Channel.Type.RoomList")]
    public interface IRoomList : IChannel
    {

        // Method
        bool GetListingRooms ();
        // Method
        void ListRooms ();
        // Method
        void StopListing ();
#if USE_DBUS_PROPERTIES
        // Property
        string Server { get; }
#endif

        event GotRoomsHandler GotRooms;

        event ListingRoomsHandler ListingRooms;

    }

    public struct RoomInfo
    {
        public uint Handle;
        public string ChannelType;
        public IDictionary<string,object> Info;
    }

    public delegate void GotRoomsHandler (RoomInfo[] @rooms);
    public delegate void ListingRoomsHandler (bool @listing);
    [Interface ("org.freedesktop.Telepathy.Channel.Type.Text")]
    public interface IText : IChannel
    {

        // Method
        void AcknowledgePendingMessages (uint[] @ids);
        // Method
        ChannelTextMessageType[] GetMessageTypes ();
        // Method
        PendingTextMessage[] ListPendingMessages (bool @clear);
        // Method
        void Send (ChannelTextMessageType @type, string @text);
#if USE_TP_PROPERTIES
        // Property
        bool anonymous { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        bool invite-only { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        uint limit { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        bool limited { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        bool moderated { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        string name { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        string description { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        string password { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        bool password-required { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        bool persistent { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        bool private { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        string subject { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        uint subject-contact { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        uint subject-timestamp { get; set; }
#endif

        event LostMessageHandler LostMessage;

        event ReceivedHandler Received;

        event SendErrorHandler SendError;

        event SentHandler Sent;

    }

    public struct PendingTextMessage
    {
        public uint Identifier;
        public uint UnixTimestamp;
        public uint Sender;
        public ChannelTextMessageType MessageType;
        public ChannelTextMessageFlags Flags;
        public string Text;
    }

    public enum ChannelTextSendError : uint
    {
        Unknown = 0, Offline = 1, InvalidContact = 2, PermissionDenied = 3, TooLong = 4, NotImplemented = 5,
    }

    public enum ChannelTextMessageType : uint
    {
        Normal = 0, Action = 1, Notice = 2, AutoReply = 3, DeliveryReport = 4,
    }

    [Flags]
    public enum ChannelTextMessageFlags : uint
    {
        None = 0,
        Truncated = 1, NonTextContent = 2, Scrollback = 4, Rescued = 8,
    }

    public delegate void LostMessageHandler ();
    public delegate void ReceivedHandler (uint @id, uint @timestamp, uint @sender, ChannelTextMessageType @type, ChannelTextMessageFlags @flags, string @text);
    public delegate void SendErrorHandler (ChannelTextSendError @error, uint @timestamp, ChannelTextMessageType @type, string @text);
    public delegate void SentHandler (uint @timestamp, ChannelTextMessageType @type, string @text);
    [Interface ("org.freedesktop.Telepathy.Channel.Type.Tubes")]
    public interface ITubes : IChannel
    {

        // Method
        IDictionary<SocketAddressType,SocketAccessControl[]> GetAvailableStreamTubeTypes ();
        // Method
        TubeType[] GetAvailableTubeTypes ();
        // Method
        TubeInfo[] ListTubes ();
        // Method
        uint OfferDBusTube (string @service, IDictionary<string,object> @parameters);
        // Method
        uint OfferStreamTube (string @service, IDictionary<string,object> @parameters, SocketAddressType @address_type, object @address, SocketAccessControl @access_control, object @access_control_param);
        // Method
        string AcceptDBusTube (uint @id);
        // Method
        object AcceptStreamTube (uint @id, SocketAddressType @address_type, SocketAccessControl @access_control, object @access_control_param);
        // Method
        void CloseTube (uint @id);
        // Method
        string GetDBusTubeAddress (uint @id);
        // Method
        DBusTubeMember[] GetDBusNames (uint @id);
        // Method
        void GetStreamTubeSocketAddress (uint @id, out SocketAddressType @address_type, out object @address);
        event NewTubeHandler NewTube;

        event TubeStateChangedHandler TubeStateChanged;

        event TubeClosedHandler TubeClosed;

        event DBusNamesChangedHandler DBusNamesChanged;

        event StreamTubeNewConnectionHandler StreamTubeNewConnection;

    }

    public struct TubeInfo
    {
        public uint Identifier;
        public uint Initiator;
        public TubeType Type;
        public string Service;
        public IDictionary<string,object> Parameters;
        public TubeState State;
    }

    public struct DBusTubeMember
    {
        public uint Handle;
        public string UniqueName;
    }

    public enum TubeType : uint
    {
        DBus = 0, Stream = 1,
    }

    public enum TubeState : uint
    {
        LocalPending = 0, RemotePending = 1, Open = 2,
    }

    public enum SocketAddressType : uint
    {
        Unix = 0, AbstractUnix = 1, IPv4 = 2, IPv6 = 3,
    }

    public enum SocketAccessControl : uint
    {
        Localhost = 0, Port = 1, Netmask = 2, Credentials = 3,
    }

    public delegate void NewTubeHandler (uint @id, uint @initiator, TubeType @type, string @service, IDictionary<string,object> @parameters, TubeState @state);
    public delegate void TubeStateChangedHandler (uint @id, TubeState @state);
    public delegate void TubeClosedHandler (uint @id);
    public delegate void DBusNamesChangedHandler (uint @id, DBusTubeMember[] @added, uint[] @removed);
    public delegate void StreamTubeNewConnectionHandler (uint @id, uint @handle);


    [Interface ("org.freedesktop.Telepathy.Channel.Type.StreamTube")]
    public interface IStreamTube : IChannel, ITube
    {

        // Method
        void Offer (SocketAddressType @address_type, object @address, SocketAccessControl @access_control, IDictionary<string,object> @parameters);
        // Method
        object Accept (SocketAddressType @address_type, SocketAccessControl @access_control, object @access_control_param);
#if USE_DBUS_PROPERTIES
        // Property
        string Service { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<SocketAddressType,SocketAccessControl[]> SupportedSocketTypes { get; }
#endif

        event NewRemoteConnectionHandler NewRemoteConnection;
        event NewLocalConnectionHandler NewLocalConnection;
        event ConnectionClosedHandler ConnectionClosed;

    }

    public delegate void NewRemoteConnectionHandler (uint @handle, object @connection_param, uint @connection_id);
    public delegate void NewLocalConnectionHandler (uint @connection_id);
    public delegate void ConnectionClosedHandler (uint @connection_id, string @error, string @message);

    [Interface ("org.freedesktop.Telepathy.Channel.Type.DBusTube")]
    public interface IDBusTube : IChannel, ITube
    {

        // Method
        string Offer (IDictionary<string,object> @parameters, SocketAccessControl @access_control);
        // Method
        string Accept (SocketAccessControl @access_control);
#if USE_DBUS_PROPERTIES
        // Property
        string ServiceName { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<uint,string> DBusNames { get; }
#endif

        event DBusNamesChangedHandler DBusNamesChanged;

    }

    //already defined for StreamTube
    //public delegate void DBusNamesChangedHandler (IDictionary<uint,string> @added, uint[] @removed);

    [Interface ("org.freedesktop.Telepathy.Channel.Type.FileTransfer")]
    public interface IFileTransfer : IChannel
    {

        // Method
        object AcceptFile (SocketAddressType @address_type, SocketAccessControl @access_control, object @access_control_param, ulong @offset);
        // Method
        object ProvideFile (SocketAddressType @address_type, SocketAccessControl @access_control, object @access_control_param);
#if USE_DBUS_PROPERTIES
        // Property
        FileTransferState State { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string ContentType { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string Filename { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ulong Size { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        FileHashType ContentHashType { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string ContentHash { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string Description { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        long Date { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<SocketAddressType,SocketAccessControl[]> AvailableSocketTypes { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ulong TransferredBytes { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ulong InitialOffset { get; }
#endif

        event FileTransferStateChangedHandler FileTransferStateChanged;

        event TransferredBytesChangedHandler TransferredBytesChanged;

        event InitialOffsetDefinedHandler InitialOffsetDefined;

    }

    public enum FileTransferState : uint
    {
        None = 0, Pending = 1, Accepted = 2, Open = 3, Completed = 4, Cancelled = 5,
    }

    public enum FileTransferStateChangeReason : uint
    {
        None = 0, Requested = 1, LocalStopped = 2, RemoteStopped = 3, LocalError = 4, RemoteError = 5,
    }

    public enum FileHashType : uint
    {
        None = 0, MD5 = 1, SHA1 = 2, SHA256 = 3,
    }

    public delegate void FileTransferStateChangedHandler (FileTransferState @state, FileTransferStateChangeReason @reason);
    public delegate void TransferredBytesChangedHandler (ulong @count);
    public delegate void InitialOffsetDefinedHandler (ulong @initialoffset);


    namespace Draft
    {

        [Interface ("org.freedesktop.Telepathy.Channel.Type.ContactSearch.DRAFT")]
        public interface IContactSearch : IChannel
        {

            // Method
            void Search (IDictionary<string,string> @terms);
            // Method
            void More ();
            // Method
            void Stop ();
    #if USE_DBUS_PROPERTIES
            // Property
            ChannelContactSearchState SearchState { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            uint Limit { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            string[] AvailableSearchKeys { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            string Server { get; }
    #endif

            event SearchStateChangedHandler SearchStateChanged;

            event SearchResultReceivedHandler SearchResultReceived;

        }

        public enum ChannelContactSearchState : uint
        {
            NotStarted = 0, InProgress = 1, MoreAvailable = 2, Completed = 3, Failed = 4,
        }

        public delegate void SearchStateChangedHandler (ChannelContactSearchState @state, string @error, IDictionary<string,object> @details);
        public delegate void SearchResultReceivedHandler (uint @contact, ContactInfoField[] @info);
    }

    [Interface ("org.freedesktop.Telepathy.Channel.Interface.CallMerging")]
    public interface ICallMerging : IStreamedMedia
    {

        // Method
        void Merge (ObjectPath @other);
        // Method
        ObjectPath Split (uint @contact);
    }

    [Interface ("org.freedesktop.Telepathy.Channel.Interface.CallState")]
    public interface ICallState : IStreamedMedia
    {

        // Method
        IDictionary<uint,ChannelCallStateFlags> GetCallStates ();
        event CallStateChangedHandler CallStateChanged;

    }

    [Flags]
    public enum ChannelCallStateFlags : uint
    {
        None = 0,
        Ringing = 1, Queued = 2, Held = 4, Forwarded = 8,
    }

    public delegate void CallStateChangedHandler (uint @contact, ChannelCallStateFlags @state);
    [Interface ("org.freedesktop.Telepathy.Channel.Interface.ChatState")]
    public interface IChatState : IChannel
    {

        // Method
        void SetChatState (ChannelChatState @state);
        event ChatStateChangedHandler ChatStateChanged;

    }

    public enum ChannelChatState : uint
    {
        Gone = 0, Inactive = 1, Active = 2, Paused = 3, Composing = 4,
    }

    public delegate void ChatStateChangedHandler (uint @contact, ChannelChatState @state);
    [Interface ("org.freedesktop.Telepathy.Channel.Interface.Destroyable")]
    public interface IDestroyable : IChannel
    {

        // Method
        void Destroy ();
    }

    [Interface ("org.freedesktop.Telepathy.Channel.Interface.DTMF")]
    public interface IDTMF : IStreamedMedia
    {

        // Method
        void StartTone (uint @stream_id, DTMFEvent @event);
        // Method
        void StopTone (uint @stream_id);
    }

    public enum DTMFEvent : byte
    {
        Digit0 = 0, Digit1 = 1, Digit2 = 2, Digit3 = 3, Digit4 = 4, Digit5 = 5, Digit6 = 6, Digit7 = 7, Digit8 = 8, Digit9 = 9, Asterisk = 10, Hash = 11, LetterA = 12, LetterB = 13, LetterC = 14, LetterD = 15,
    }

    [Interface ("org.freedesktop.Telepathy.Channel.Interface.Group")]
    public interface IGroup : IChannel
    {

        // Method
        void AddMembers (uint[] @contacts, string @message);
        // Method
        void GetAllMembers (out uint[] @members, out uint[] @local_pending, out uint[] @remote_pending);
        // Method
        ChannelGroupFlags GetGroupFlags ();
        // Method
        uint[] GetHandleOwners (uint[] @handles);
        // Method
        uint[] GetLocalPendingMembers ();
        // Method
        LocalPendingInfo[] GetLocalPendingMembersWithInfo ();
        // Method
        uint[] GetMembers ();
        // Method
        uint[] GetRemotePendingMembers ();
        // Method
        uint GetSelfHandle ();
        // Method
        void RemoveMembers (uint[] @contacts, string @message);
        // Method
        void RemoveMembersWithReason (uint[] @contacts, string @message, ChannelGroupChangeReason @reason);
#if USE_DBUS_PROPERTIES
        // Property
        ChannelGroupFlags GroupFlags { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<uint,uint> HandleOwners { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        LocalPendingInfo[] LocalPendingMembers { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint[] Members { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint[] RemotePendingMembers { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint SelfHandle { get; }
#endif

        event HandleOwnersChangedHandler HandleOwnersChanged;

        event SelfHandleChangedHandler SelfHandleChanged;

        event GroupFlagsChangedHandler GroupFlagsChanged;

        event MembersChangedHandler MembersChanged;

        event MembersChangedDetailedHandler MembersChangedDetailed;

    }

    public struct LocalPendingInfo
    {
        public uint ToBeAdded;
        public uint Actor;
        public ChannelGroupChangeReason Reason;
        public string Message;
    }

    public enum ChannelGroupChangeReason : uint
    {
        None = 0, Offline = 1, Kicked = 2, Busy = 3, Invited = 4, Banned = 5, Error = 6, InvalidContact = 7, NoAnswer = 8, Renamed = 9, PermissionDenied = 10, Separated = 11,
    }

    [Flags]
    public enum ChannelGroupFlags : uint
    {
        None = 0,
        CanAdd = 1, CanRemove = 2, CanRescind = 4, MessageAdd = 8, MessageRemove = 16, MessageAccept = 32, MessageReject = 64, MessageRescind = 128, ChannelSpecificHandles = 256, OnlyOneGroup = 512, HandleOwnersNotAvailable = 1024, Properties = 2048, MembersChangedDetailed = 4096, MessageDepart = 8192,
    }

    public delegate void HandleOwnersChangedHandler (IDictionary<uint,uint> @added, uint[] @removed);
    public delegate void GroupFlagsChangedHandler (ChannelGroupFlags @added, ChannelGroupFlags @removed);
    public delegate void MembersChangedHandler (string @message, uint[] @added, uint[] @removed, uint[] @local_pending, uint[] @remote_pending, uint @actor, ChannelGroupChangeReason @reason);
    public delegate void MembersChangedDetailedHandler (uint[] @added, uint[] @removed, uint[] @local_pending, uint[] @remote_pending, IDictionary<string,object> @details);
    [Interface ("org.freedesktop.Telepathy.Channel.Interface.Hold")]
    public interface IHold : IStreamedMedia
    {

        // Method
        void GetHoldState (out LocalHoldState @holdstate, out LocalHoldStateReason @reason);
        // Method
        void RequestHold (bool @hold);
        event HoldStateChangedHandler HoldStateChanged;

    }

    public enum LocalHoldState : uint
    {
        Unheld = 0, Held = 1, PendingHold = 2, PendingUnhold = 3,
    }

    public enum LocalHoldStateReason : uint
    {
        None = 0, Requested = 1, ResourceNotAvailable = 2,
    }

    public delegate void HoldStateChangedHandler (LocalHoldState @holdstate, LocalHoldStateReason @reason);


    namespace Draft
    {

        [Interface ("org.freedesktop.Telepathy.Channel.Interface.HTML.DRAFT")]
        public interface IHTML : IText, IMessages
        {

        }

    }

    [Interface ("org.freedesktop.Telepathy.Channel.Interface.Password")]
    public interface IPassword : IChannel
    {

        // Method
        ChannelPasswordFlags GetPasswordFlags ();
        // Method
        bool ProvidePassword (string @password);
        event PasswordFlagsChangedHandler PasswordFlagsChanged;

    }

    [Flags]
    public enum ChannelPasswordFlags : uint
    {
        None = 0,
        Provide = 8,
    }

    public delegate void PasswordFlagsChangedHandler (ChannelPasswordFlags @added, ChannelPasswordFlags @removed);
    [Interface ("org.freedesktop.Telepathy.Channel.Interface.MediaSignalling")]
    public interface IMediaSignalling : IChannel, IStreamedMedia
    {

        // Method
        MediaSessionHandlerInfo[] GetSessionHandlers ();
#if USE_TP_PROPERTIES
        // Property
        string nat-traversal { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        string stun-server { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        short stun-port { get; set; }
#endif

#if USE_TP_PROPERTIES
        // Property
        string gtalk-p2p-relay-token { get; set; }
#endif

        event NewSessionHandlerHandler NewSessionHandler;

    }

    public struct MediaSessionHandlerInfo
    {
        public ObjectPath SessionHandler;
        public string MediaSessionType;
    }

    public delegate void NewSessionHandlerHandler (ObjectPath @session_handler, string @session_type);
    [Interface ("org.freedesktop.Telepathy.Channel.Interface.MediaSignalling.FUTURE")]
    public interface IMediaSignallingFuture : IChannel, IStreamedMedia, IMediaSignalling
    {

#if USE_DBUS_PROPERTIES
        // Property
        bool ICETransportAvailable { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool RawUDPTransportAvailable { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool GTalkP2PTransportAvailable { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool WLM85TransportAvailable { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool WLM2009TransportAvailable { get; }
#endif

    }

    [Interface ("org.freedesktop.Telepathy.Channel.Interface.Messages")]
    public interface IMessages : IText
    {

        // Method
        string SendMessage (IDictionary<string,object>[] @message, MessageSendingFlags @flags);
        // Method
        IDictionary<uint,object> GetPendingMessageContent (uint @message_id, uint[] @parts);
#if USE_DBUS_PROPERTIES
        // Property
        string[] SupportedContentTypes { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        MessagePartSupportFlags MessagePartSupportFlags { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<string,object>[] PendingMessages { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        DeliveryReportingSupportFlags DeliveryReportingSupport { get; }
#endif

        event MessageSentHandler MessageSent;

        event PendingMessagesRemovedHandler PendingMessagesRemoved;

        event MessageReceivedHandler MessageReceived;

    }

    public enum DeliveryStatus : uint
    {
        Unknown = 0, Delivered = 1, TemporarilyFailed = 2, PermanentlyFailed = 3, Accepted = 4,
    }

    [Flags]
    public enum MessagePartSupportFlags : uint
    {
        None = 0,
        OneAttachment = 1, MultipleAttachments = 2,
    }

    [Flags]
    public enum MessageSendingFlags : uint
    {
        None = 0,
        ReportDelivery = 1,
    }

    [Flags]
    public enum DeliveryReportingSupportFlags : uint
    {
        None = 0,
        ReceiveFailures = 1, ReceiveSuccesses = 2,
    }

    public delegate void MessageSentHandler (IDictionary<string,object>[] @content, MessageSendingFlags @flags, string @message_token);
    public delegate void PendingMessagesRemovedHandler (uint[] @message_ids);
    public delegate void MessageReceivedHandler (IDictionary<string,object>[] @message);


    [Interface ("org.freedesktop.Telepathy.Channel.Interface.Tube")]
    public interface ITube : IChannel
    {

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<string,object> Parameters { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        TubeChannelState State { get; }
#endif

        event TubeChannelStateChangedHandler TubeChannelStateChanged;

    }

    public enum TubeChannelState : uint
    {
        LocalPending = 0, RemotePending = 1, Open = 2, NotOffered = 3,
    }

    public delegate void TubeChannelStateChangedHandler (TubeChannelState @state);



    [Interface ("org.freedesktop.Telepathy.Media.SessionHandler")]
    public interface ISessionHandler
    {

        // Method
        void Error (MediaStreamError @error_code, string @message);
        // Method
        void Ready ();
        event NewStreamHandlerHandler NewStreamHandler;

    }

    public delegate void NewStreamHandlerHandler (ObjectPath @stream_handler, uint @id, MediaStreamType @media_type, MediaStreamDirection @direction);
    [Interface ("org.freedesktop.Telepathy.Media.StreamHandler")]
    public interface IStreamHandler
    {

        // Method
        void CodecChoice (uint @codec_id);
        // Method
        void Error (MediaStreamError @error_code, string @message);
        // Method
        void NativeCandidatesPrepared ();
        // Method
        void NewActiveCandidatePair (string @native_candidate_id, string @remote_candidate_id);
        // Method
        void NewNativeCandidate (string @candidate_id, MediaStreamHandlerTransport[] @transports);
        // Method
        void Ready (MediaStreamHandlerCodec[] @codecs);
        // Method
        void SetLocalCodecs (MediaStreamHandlerCodec[] @codecs);
        // Method
        void StreamState (MediaStreamState @state);
        // Method
        void SupportedCodecs (MediaStreamHandlerCodec[] @codecs);
        // Method
        void CodecsUpdated (MediaStreamHandlerCodec[] @codecs);
        // Method
        void HoldState (bool @held);
        // Method
        void UnholdFailure ();
#if USE_DBUS_PROPERTIES
        // Property
        SocketAddressIP[] STUNServers { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool CreatedLocally { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string NATTraversal { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<string,object>[] RelayInfo { get; }
#endif

        event AddRemoteCandidateHandler AddRemoteCandidate;

        event CloseHandler Close;

        event RemoveRemoteCandidateHandler RemoveRemoteCandidate;

        event SetActiveCandidatePairHandler SetActiveCandidatePair;

        event SetRemoteCandidateListHandler SetRemoteCandidateList;

        event SetRemoteCodecsHandler SetRemoteCodecs;

        event SetStreamPlayingHandler SetStreamPlaying;

        event SetStreamSendingHandler SetStreamSending;

        event StartTelephonyEventHandler StartTelephonyEvent;

        event StopTelephonyEventHandler StopTelephonyEvent;

        event SetStreamHeldHandler SetStreamHeld;

    }

    public struct SocketAddressIP                 // manually added
    {
        public string Address;
        public short Port;
    }

    public struct MediaStreamHandlerCandidate
    {
        public string Name;
        public MediaStreamHandlerTransport[] Transports;
    }

    public struct MediaStreamHandlerTransport
    {
        public uint ComponentNumber;
        public string IPAddress;
        public uint Port;
        public MediaStreamBaseProto Protocol;
        public string Subtype;
        public string Profile;
        public double PreferenceValue;
        public MediaStreamTransportType TransportType;
        public string Username;
        public string Password;
    }

    public struct MediaStreamHandlerCodec
    {
        public uint CodecID;
        public string Name;
        public MediaStreamType MediaType;
        public uint ClockRate;
        public uint NumberOfChannels;
        public IDictionary<string,string> Parameters;
    }

    public enum MediaStreamError : uint
    {
        Unknown = 0, EOS = 1,
    }

    public enum MediaStreamBaseProto : uint
    {
        UDP = 0, TCP = 1,
    }

    public enum MediaStreamTransportType : uint
    {
        Local = 0, Derived = 1, Relay = 2,
    }

    public delegate void AddRemoteCandidateHandler (string @candidate_id, MediaStreamHandlerTransport[] @transports);
    public delegate void CloseHandler ();
    public delegate void RemoveRemoteCandidateHandler (string @candidate_id);
    public delegate void SetActiveCandidatePairHandler (string @native_candidate_id, string @remote_candidate_id);
    public delegate void SetRemoteCandidateListHandler (MediaStreamHandlerCandidate[] @remote_candidates);
    public delegate void SetRemoteCodecsHandler (MediaStreamHandlerCodec[] @codecs);
    public delegate void SetStreamPlayingHandler (bool @playing);
    public delegate void SetStreamSendingHandler (bool @sending);
    public delegate void StartTelephonyEventHandler (byte @event);
    public delegate void StopTelephonyEventHandler ();
    public delegate void SetStreamHeldHandler (bool @held);

    namespace Draft
    {
        [Interface ("org.freedesktop.Telepathy.Debug.DRAFT")]
        public interface IDebug
        {
            DebugMessage [] GetMessages ();

            event NewDebugMessageHandler NewDebugMessage;

#if USE_DBUS_PROPERTIES
            bool Enabled { get; }
#endif
        }

        public struct DebugMessage
        {
            public double Timestamp;
            public string Domain;
            public DebugLevel Level;
            public string Message;
        }

        public enum DebugLevel : uint {
            Error = 0, Critical = 1, Warning = 2, Message = 3, Info = 4, Debug = 5
        };

        public delegate void NewDebugMessageHandler (double @date, string @domain, DebugLevel @level, string @message);
    }

    [Interface ("org.freedesktop.Telepathy.AccountManager")]
    public interface IAccountManager
    {

        // Method
        ObjectPath CreateAccount (string @connection_manager, string @protocol, string @display_name, IDictionary<string,object> @parameters);
#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath[] ValidAccounts { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath[] InvalidAccounts { get; }
#endif

        event AccountRemovedHandler AccountRemoved;

        event AccountValidityChangedHandler AccountValidityChanged;

    }

    public delegate void AccountRemovedHandler (ObjectPath @account);
    public delegate void AccountValidityChangedHandler (ObjectPath @account, bool @valid);

    [Interface ("org.freedesktop.Telepathy.Account")]
    public interface IAccount
    {

        // Method
        void Remove ();
        // Method
        void UpdateParameters (IDictionary<string,object> @set, string[] @unset);
#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string DisplayName { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string Icon { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool Valid { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool Enabled { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string Nickname { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<string,object> Parameters { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        SimplePresence AutomaticPresence { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool ConnectAutomatically { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath Connection { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint ConnectionStatus { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        uint ConnectionStatusReason { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        SimplePresence CurrentPresence { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        SimplePresence RequestedPresence { get; set; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string NormalizedName { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        bool HasBeenOnline { get; }
#endif

        event RemovedHandler Removed;

        event AccountPropertyChangedHandler AccountPropertyChanged;

    }

    public delegate void RemovedHandler ();
    public delegate void AccountPropertyChangedHandler (IDictionary<string,object> @properties);

    [Interface ("org.freedesktop.Telepathy.Account.Interface.Avatar")]
    public interface IAvatar : IAccount
    {

#if USE_DBUS_PROPERTIES
        // Property
        Avatar Avatar { get; set; }
#endif

        event AvatarChangedHandler AvatarChanged;

    }

    public struct Avatar
    {
        public byte[] AvatarData;
        public string MIMEType;
    }

    public delegate void AvatarChangedHandler ();

    [Interface ("org.freedesktop.Telepathy.ChannelDispatcher")]
    public interface IChannelDispatcher
    {

        // Method
        ObjectPath CreateChannel (ObjectPath @account, IDictionary<string,object> @requested_properties, long @user_action_time, string @preferred_handler);
        // Method
        ObjectPath EnsureChannel (ObjectPath @account, IDictionary<string,object> @requested_properties, long @user_action_time, string @preferred_handler);
#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

    }

    [Interface ("org.freedesktop.Telepathy.ChannelDispatcher.Interface.OperationList")]
    public interface IOperationList : IChannelDispatcher
    {

#if USE_DBUS_PROPERTIES
        // Property
        DispatchOperationDetails[] DispatchOperations { get; }
#endif

        event NewDispatchOperationHandler NewDispatchOperation;

        event DispatchOperationFinishedHandler DispatchOperationFinished;

    }

    public struct DispatchOperationDetails
    {
        public ObjectPath ChannelDispatchOperation;
        public IDictionary<string,object> Properties;
    }

    public delegate void NewDispatchOperationHandler (ObjectPath @dispatch_operation, IDictionary<string,object> @properties);
    public delegate void DispatchOperationFinishedHandler (ObjectPath @dispatch_operation);

    [Interface ("org.freedesktop.Telepathy.ChannelDispatchOperation")]
    public interface IChannelDispatchOperation
    {

        // Method
        void HandleWith (string @handler);
        // Method
        void Claim ();
#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath Connection { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath Account { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        ChannelDetails[] Channels { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string[] PossibleHandlers { get; }
#endif

        event ChannelLostHandler ChannelLost;

        event FinishedHandler Finished;

    }

    public delegate void ChannelLostHandler (ObjectPath @channel, string @error, string @message);
    public delegate void FinishedHandler ();

    [Interface ("org.freedesktop.Telepathy.ChannelRequest")]
    public interface IChannelRequest
    {

        // Method
        void Proceed ();
        // Method
        void Cancel ();
#if USE_DBUS_PROPERTIES
        // Property
        ObjectPath Account { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        long UserActionTime { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string PreferredHandler { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        IDictionary<string,object>[] Requests { get; }
#endif

#if USE_DBUS_PROPERTIES
        // Property
        string[] Interfaces { get; }
#endif

        event FailedHandler Failed;

        event SucceededHandler Succeeded;

    }

    public delegate void FailedHandler (string @error, string @message);
    public delegate void SucceededHandler ();


    namespace Client
    {
        [Interface ("org.freedesktop.Telepathy.Client")]
        public interface IClient
        {

    #if USE_DBUS_PROPERTIES
            // Property
            string[] Interfaces { get; }
    #endif

        }

        [Interface ("org.freedesktop.Telepathy.Client.Observer")]
        public interface IObserver : IClient
        {

            // Method
            void ObserveChannels (ObjectPath @account, ObjectPath @connection, ChannelDetails[] @channels, ObjectPath @dispatch_operation, ObjectPath[] @requests_satisfied, IDictionary<string,object> @observer_info);
    #if USE_DBUS_PROPERTIES
            // Property
            IDictionary<string,object>[] ObserverChannelFilter { get; }
    #endif

        }

        [Interface ("org.freedesktop.Telepathy.Client.Approver")]
        public interface IApprover : IClient
        {

            // Method
            void AddDispatchOperation (ChannelDetails[] @channels, ObjectPath @dispatchoperation, IDictionary<string,object> @properties);
    #if USE_DBUS_PROPERTIES
            // Property
            IDictionary<string,object>[] ApproverChannelFilter { get; }
    #endif

        }

        [Interface ("org.freedesktop.Telepathy.Client.Handler")]
        public interface IHandler : IClient
        {

            // Method
            void HandleChannels (ObjectPath @account, ObjectPath @connection, ChannelDetails[] @channels, ObjectPath[] @requests_satisfied, ulong @user_action_time, IDictionary<string,object> @handler_info);
    #if USE_DBUS_PROPERTIES
            // Property
            IDictionary<string,object>[] HandlerChannelFilter { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            bool BypassApproval { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            ObjectPath[] HandledChannels { get; }
    #endif

    #if USE_DBUS_PROPERTIES
            // Property
            string[] Capabilities { get; }
    #endif

        }

        [Interface ("org.freedesktop.Telepathy.Client.Interface.Requests")]
        public interface IRequests : IClient, IHandler
        {

            // Method
            void AddRequest (ObjectPath @request, IDictionary<string,object> @properties);
            // Method
            void RemoveRequest (ObjectPath @request, string @error, string @message);
        }

    } // end Client namespace

    [Interface ("org.freedesktop.Telepathy.ChannelHandler")]
    public interface IChannelHandler
    {

        // Method
        void HandleChannel (string @bus_name, ObjectPath @connection, string @channel_type, ObjectPath @channel, HandleType @handle_type, uint @handle);
    }

    [Interface ("org.freedesktop.Telepathy.Properties")]
    public interface IProperties
    {

        // Method
        PropertyValue[] GetProperties (uint[] @properties);
        // Method
        PropertySpec[] ListProperties ();
        // Method
        void SetProperties (PropertyValue[] @properties);
        event PropertiesChangedHandler PropertiesChanged;

        event PropertyFlagsChangedHandler PropertyFlagsChanged;

    }

    public struct PropertySpec
    {
        public uint PropertyID;
        public string Name;
        public string Signature;
        public PropertyFlags Flags;
    }

    public struct PropertyFlagsChange
    {
        public uint PropertyID;
        public uint NewFlags;
    }

    public struct PropertyValue
    {
        public uint Identifier;
        public object Value;
    }

    [Flags]
    public enum PropertyFlags : uint
    {
        None = 0,
        Read = 1, Write = 2,
    }

    public delegate void PropertiesChangedHandler (PropertyValue[] @properties);
    public delegate void PropertyFlagsChangedHandler (PropertyFlagsChange[] @properties);
}
