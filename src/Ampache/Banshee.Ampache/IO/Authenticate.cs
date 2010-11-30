//
// Authenticate.cs
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
// OUT OF OR IN C

using System;
using System.Xml.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Net;
using System.Linq;

namespace Banshee.Ampache
{
    public class Authenticate : Handshake
    {
        const string REQUEST = "{0}/server/xml.server.php?action=handshake&auth={1}&timestamp={2}&version=350001&user={3}";
        const string PING = "{0}/server/xml.server.php?action=ping&auth={1}";

        public Authenticate(string server, string user, string password) : base ()
        {
            if(string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("must provide a server/web site name", "server");
            }
            Server = server;
            if(string.IsNullOrEmpty(user))
            {
                throw new ArgumentException("must provide a user name", "user");
            }
            User = user;
            if(string.IsNullOrEmpty(server))
            {
                throw new ArgumentException("must provide a password", "password");
            }
            if(!AuthenticateToServer(password))
            {
                throw new ArgumentException("Invalid username password combination");
            }
        }

        private bool AuthenticateToServer(string password)
        {
            byte[] passBytes = Encoding.UTF8.GetBytes(password);
            var hasher = new SHA256Managed();
            var tmpBytes = hasher.ComputeHash(passBytes);

            var hashword = HexString(tmpBytes);
            var now = DateTime.Now.UnixEpoch();
            tmpBytes = Encoding.UTF8.GetBytes(now + hashword);
            tmpBytes = hasher.ComputeHash(tmpBytes);

            hashword = HexString(tmpBytes);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(REQUEST, Server, hashword, now, User));
            var response = request.GetResponse();
            var result = XElement.Load(new StreamReader(response.GetResponseStream()));
            if(result.Descendants("error").Count() == 0 && result.Descendants("auth").FirstOrDefault() != null) {
                Passphrase = result.Descendants("auth").First().Value;
                SongCount = int.Parse(result.Descendants("songs").First().Value);
                ArtistCount = int.Parse(result.Descendants("artists").First().Value);
                AlbumCount = int.Parse(result.Descendants("albums").First().Value);
                return true;
            }
            return false;
        }

        public void Ping()
        {
            var request = (HttpWebRequest)WebRequest.Create(string.Format(PING, Server, Passphrase));
            request.GetResponse();
        }

        private string HexString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }

    public static class DateTimeExtensions
    {
        public static int UnixEpoch(this DateTime time)
        {
            var unixEpoch = new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc);
            return Convert.ToInt32(time.ToUniversalTime().Subtract(unixEpoch).TotalSeconds);
        }
    }
}
