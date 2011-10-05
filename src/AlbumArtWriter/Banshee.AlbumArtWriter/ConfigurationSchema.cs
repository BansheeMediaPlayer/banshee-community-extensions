// 
// ConfigurationSchema.cs
// 
// Author:
//   Kevin Anthony <Kevin.S.Anthony@gmail.com>
// 
// Copyright (C) 2011 Kevin Anthony
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

using Banshee.Configuration;

namespace Banshee.AlbumArtWriter
{
    public static class ConfigurationSchema
    {
        private const string conf_namespace = "plugins.alarm";

        public static readonly SchemaEntry<bool> IsEnabled
            = new SchemaEntry<bool> (
                conf_namespace, "is_enabled",
                false, "Enable the Album Writer plugin",
                ""
            );

        public static readonly SchemaEntry<bool> JPG
            = new SchemaEntry<bool> (
                conf_namespace, "write_jpg",
                true, "Write Art as JPG",
                ""
            );

        public static readonly SchemaEntry<bool> PNG
            = new SchemaEntry<bool> (
                conf_namespace, "write_png",
                false, "Write Art as PNG",
                ""
            );       public static readonly SchemaEntry<string> ArtName
            = new SchemaEntry<string> (
                conf_namespace, "art_name",
                "album", "The Name (Without Extension) to write the art",
                ""
            );

    }
}

