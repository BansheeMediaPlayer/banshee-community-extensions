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

#define USE_DBUS_PROPERTIES

using System;
using System.Collections.Generic;
using DBus;

namespace Telepathy.MissionControl
{

    [Interface ("org.freedesktop.Telepathy.MissionControl")]
    public interface IMissionControl
    {
        string[] GetOnlineConnections ();
        void GetConnection (string @account_name, out string @bus_name, out ObjectPath @object_path);
        McStatus GetConnectionStatus (string @account_name);

        string GetPresenceMessageActual ();
        McPresence GetPresenceActual ();
        string GetPresenceMessage ();
        McPresence GetPresence ();
        void SetPresence (McPresence @presence, string message);

        string GetAccountForConnection (string @object_path);

        event StatusActualHandler StatusActual;
        event PresenceStatusActualHandler PresenceStatusActual;
        event PresenceChangedHandler PresenceChanged;
        event PresenceStatusRequestedHandler PresenceStatusRequested;
        event PresenceRequestedHandler PresenceRequested;
        event AccountStatusChangedHandler AccountStatusChanged;
        event AccountPresenceChangedHandler AccountPresenceChanged;

    }

    public struct McAccountStatus
    {
        public string UniqueName;
        public Telepathy.ConnectionStatus status;
        public McPresence presence;
        public Telepathy.ConnectionStatusReason reason;
    }

    public enum McPresence : uint
    {
        Unset = 0,
        Offline = 1, Available = 2, Away = 3, ExtendedAway = 4, Hidden = 5,
        DoNotDisturb = 6, Last = 7
    }

    public enum McStatus : uint
    {
        Connected = 0, Connecting = 1, Disconnected = 2,
    }

    public delegate void StatusActualHandler (McStatus @status, McPresence @presence);
    public delegate void PresenceStatusActualHandler (McPresence @presence);
    public delegate void PresenceChangedHandler (McPresence @presence, string @message);
    public delegate void PresenceStatusRequestedHandler (McPresence @presence);
    public delegate void PresenceRequestedHandler (McPresence @presence, string @message);
    public delegate void AccountStatusChangedHandler (McStatus @status, McPresence @presence, ConnectionStatusReason @reason, string @account_id);
    public delegate void AccountPresenceChangedHandler (McStatus @status, McPresence @presence, string @message, ConnectionStatusReason @reason, string @account_id);
}