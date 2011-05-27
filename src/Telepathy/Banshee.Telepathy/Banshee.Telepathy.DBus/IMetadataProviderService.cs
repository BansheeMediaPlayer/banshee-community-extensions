//
// IMetadataProviderService.cs
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
using DBus;

namespace Banshee.Telepathy.DBus
{

    [Interface ("org.bansheeproject.MetadataProviderService")]
    public interface IMetadataProviderService
    {
        event PermissionSetHandler PermissionSet;
        event DownloadingAllowedHandler DownloadingAllowedChanged;
        event StreamingAllowedHandler StreamingAllowedChanged;

        ObjectPath CreateMetadataProvider (LibraryType type);
        ObjectPath CreatePlaylistProvider (int id);
        int[] GetPlaylistIds (LibraryType type);
        bool PermissionGranted ();
        void RequestPermission ();
        void DownloadFile (long external_id, string content_type);
        string GetTrackPath (long id);
        bool DownloadsAllowed ();
        bool StreamingAllowed ();

        //void Destroy ();
    }

}