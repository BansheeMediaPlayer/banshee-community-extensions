//
// EmpathyHandler.cs
//
// Author:
//   Neil Loknath <neil.loknath@gmail.com>
//
// Copyright (C) 2009 Neil Loknath
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using DBus;

namespace Banshee.Telepathy.API.DBus
{
    public static class EmpathyConstants
    {
        public const string DTUBE_HANDLER_IFACE = "org.gnome.Empathy.DTubeHandler";
        public const string DTUBE_HANDLER_PATH = "/org/gnome/Empathy/DTubeHandler";

        public const string STREAMTUBE_HANDLER_IFACE = "org.gnome.Empathy.StreamTubeHandler";
        public const string STREAMTUBE_HANDLER_PATH = "/org/gnome/Empathy/StreamTubeHandler";
    }

    // Empathy specific interface to tell Empathy we are handling the tube
    [Interface ("org.gnome.Empathy.TubeHandler")]
    public interface ITubeHandler
    {
        void HandleTube (string bus_name,
                         ObjectPath conn,
                         ObjectPath channel,
                         uint handle_type,
                         uint handle);
    }

    public class EmpathyHandler : ITubeHandler
    {
        public void HandleTube (string bus_name,
                                ObjectPath conn,
                                ObjectPath channel,
                                uint handle_type,
                                uint handle)
        {
            return;
        }
    }
}