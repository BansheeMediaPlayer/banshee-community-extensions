//
// FileTransferDispatcher.cs
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
using System.Collections.Generic;

using Telepathy;

using Banshee.Telepathy.API.Data;
using Banshee.Telepathy.API.Channels;
using Banshee.Telepathy.API.Dispatchables;

namespace Banshee.Telepathy.API.Dispatchers
{
    internal sealed class FileTransferDispatcher : Dispatcher
    {
        internal FileTransferDispatcher (Connection conn) : base (conn,
                                                                  Constants.CHANNEL_TYPE_FILETRANSFER,
                                                                  new string [] { "Filename", "ContentType", "Size", "Description" } )
        {
            DispatchObject = typeof (OutgoingFileTransfer);
            PropertyKeysForDispatching = new string [] { "Filename" };
        }

        protected override bool CanProcess (ChannelDetails details)
        {
            foreach (FileTransferChannelInfo info in Connection.SupportedChannels.GetAll<FileTransferChannelInfo> ()) {
                string content_type = (string) details.Properties[Constants.CHANNEL_TYPE_FILETRANSFER + ".ContentType"];
                string desc = (string) details.Properties[Constants.CHANNEL_TYPE_FILETRANSFER + ".Description"];
                if (info.ContentType.Equals (content_type) && info.Description.Equals (desc)) {
                    return true;
                }
            }

            return false;
        }

        protected override void ProcessNewChannel (string object_path,
                                                   uint initiator_handle,
                                                   uint target_handle,
                                                   ChannelDetails c)
        {
            Console.WriteLine ("Processing new channel for file transfer");

            string filename = (string) c.Properties[Constants.CHANNEL_TYPE_FILETRANSFER + ".Filename"];
            string content_type = (string) c.Properties[Constants.CHANNEL_TYPE_FILETRANSFER + ".ContentType"];
            ulong size = (ulong) c.Properties[Constants.CHANNEL_TYPE_FILETRANSFER + ".Size"];
            Contact contact = Connection.Roster.GetContact (target_handle);

            FileTransferChannel ft = null;
            FileTransfer transfer = null;

            try {
                ft = new FileTransferChannel (this.Connection,
                                              object_path,
                                              initiator_handle,
                                              target_handle,
                                              filename,
                                              content_type,
                                             (long) size);

                if (initiator_handle != Connection.SelfHandle) {
                    transfer = new IncomingFileTransfer (contact, ft);
                }
                else {
                    transfer = new OutgoingFileTransfer (contact, ft);
                }

                if (transfer != null) {
                    DispatchManager dm = Connection.DispatchManager;
                    dm.Add (contact, transfer.OriginalFilename, transfer);
                }
            }
            catch (Exception e) {
                Console.WriteLine (e.ToString ());

                if (transfer != null) {
                    transfer.Dispose ();
                } else if (ft != null) {
                    ft.Dispose ();
                }
            }
        }
    }
}