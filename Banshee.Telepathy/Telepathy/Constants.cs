using System;

namespace Telepathy
{
    public struct Constants
    {
        public const string CONNMANAGER_GABBLE_IFACE = "org.freedesktop.Telepathy.ConnectionManager.gabble";
        public const string CONNMANAGER_GABBLE_PATH = "/org/freedesktop/Telepathy/ConnectionManager/gabble";

        public const string CONNECTION_IFACE = "org.freedesktop.Telepathy.Connection";
        public const string AVATAR_IFACE = "org.freedesktop.Telepathy.Interface.Avatars";
        public const string REQUESTS_IFACE = "org.freedesktop.Telepathy.Connection.Interface.Requests";
        public const string CHANNEL_IFACE = "org.freedesktop.Telepathy.Channel";
        public const string CHANNEL_TYPE_CONTACTLIST = "org.freedesktop.Telepathy.Channel.Type.ContactList";
        public const string CHANNEL_TYPE_DBUSTUBE = "org.freedesktop.Telepathy.Channel.Type.DBusTube";
        public const string CHANNEL_TYPE_STREAMTUBE = "org.freedesktop.Telepathy.Channel.Type.StreamTube";
        public const string CHANNEL_TYPE_TEXT = "org.freedesktop.Telepathy.Channel.Type.Text";
        public const string CHANNEL_TYPE_FILETRANSFER = "org.freedesktop.Telepathy.Channel.Type.FileTransfer";
        
        public const string MISSIONCONTROL_IFACE = "org.freedesktop.Telepathy.MissionControl";
        public const string MISSIONCONTROL_PATH = "/org/freedesktop/Telepathy/MissionControl";

        public const string DBUS_PROPERTIES = "org.freedesktop.DBus.Properties";
    }
}
