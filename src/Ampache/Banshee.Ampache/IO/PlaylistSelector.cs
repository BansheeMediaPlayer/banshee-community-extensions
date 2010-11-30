//
// AmpacheSelectorBase.cs
//
// Author:
//       John Moore <jcwmoore@gmail.com>
//
// Copyright (c) 2010 John Moore
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml.Linq;
using System.IO;
using System.Linq;
namespace Banshee.Ampache
{
    internal class PlaylistSelector : AmpacheSelectorBase<AmpachePlaylist>
    {
        private const string NODE_NAME = @"playlist";
        private const string ACTION = @"playlists";
        private const string SONG_ACTION = @"playlist_songs";
        private static Dictionary<Type, string> MAP = new Dictionary<Type, string>();
        private readonly IEntityFactory<AmpacheSong> _songFactory;

        public PlaylistSelector (Handshake handshake, IEntityFactory<AmpachePlaylist> factory,  IEntityFactory<AmpacheSong> songFactory)
            : base (handshake, factory)
        {
            _songFactory = songFactory;
        }
        #region implemented abstract members of Banshee.Ampache.AmpacheSelectorBase[Banshee.Ampache.AmpachePlaylist]

        protected override string SelectAllMethod { get { return ACTION;} }

        protected override string XmlNodeName { get { return NODE_NAME; } }

        protected override Dictionary<Type, string> SelectMethodMap { get { return MAP; } }

        #endregion

        public override IEnumerable<AmpachePlaylist> SelectAll ()
        {
            var results = base.SelectAll ();
            foreach (var playlist in results)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat(BASE_URL, _handshake.Server, SONG_ACTION, _handshake.Passphrase);
                builder.AppendFormat(FILTER_PARAMETER, playlist.Id);
                var request = (HttpWebRequest)WebRequest.Create (builder.ToString());
                var response = request.GetResponse();
                var raw = XElement.Load(new StreamReader(response.GetResponseStream()));
                playlist.Songs = _songFactory.Construct(raw.Descendants("song").ToList()).ToList();
                yield return playlist;
            }
        }
    }
}
