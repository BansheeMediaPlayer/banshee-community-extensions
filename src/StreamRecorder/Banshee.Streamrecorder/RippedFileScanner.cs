//
// RippedFileScanner.cs
//
// Author:
//   Akseli Mantila <aksu@paju.oulu.fi>
//
// Copyright (C) 2009 Akseli Mantila
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

using System.IO;
using System.Collections.Generic;
using System.Threading;

using Hyena;

using Banshee.Streaming;
using Banshee.Collection.Database;

namespace Banshee.Streamrecorder
{

    /// <summary>
    /// Ripped File Scanner written by Akseli Mantila to integrate recorded files into the music library
    /// </summary>
    public class RippedFileScanner
    {
        public static Thread folder_scanner = null;
        public static bool closing = false;
        public static string basepath;
        public static List<string> current_list = null;
        public static List<string> previous_list = null;

        public static void SetScanDirectory (string path)
        {
            basepath = path;
        }

        public static void StartScanner ()
        {
            Hyena.Log.DebugFormat ("[RippedFileScanner] <StartScanner> Started. Dir: {0}", basepath);

            if (folder_scanner == null) {
                folder_scanner = new Thread (ScannerThread);

                if (Directory.Exists (basepath)) {
                    RippedFileScanner.SetScanDirectory (basepath);
                    folder_scanner.Start ();
                }
            }
        }

        public static void StopScanner ()
        {
            if (folder_scanner != null && folder_scanner.IsAlive) {
                try {
                    folder_scanner.Abort ();
                } catch {
                }
            }

            folder_scanner = null;
        }

        public static void ScannerThread ()
        {
            Hyena.Log.Debug ("[RippedFileScanner] <ScannerThread> Started");

            while (!closing) {
                Scan ();
                Thread.Sleep (15 * 1000);
            }

            Hyena.Log.Debug ("[RippedFileScanner] <ScannerThread> Stopped");
        }

        public static void Scan ()
        {
            Hyena.Log.Debug ("[RippedFileScanner] <Scan> Start");

            List<string> current_list = new List<string> ();
            GetFileNames (current_list, basepath);
            List<string> new_items = GetNewItems (previous_list, current_list);

            if (new_items != null && previous_list != null) {
                foreach (string item in new_items) {
                    DatabaseTrackInfo new_track = new DatabaseTrackInfo ();

                    if (new_track != null) {
                        StreamTagger.TrackInfoMerge (new_track, new SafeUri (item));
                        // I think here should be a check to database if track is unique
                        Hyena.Log.DebugFormat ("[RippedFileScanner] <Scan> New track found! Artist: {0} Title: {1}", new_track.ArtistName, new_track.TrackTitle);
                        new_track.PrimarySource = Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary;
                        new_track.Save ();
                    }
                }
            }

            previous_list = current_list;
            Hyena.Log.Debug ("[RippedFileScanner] <Scan> End");
        }

        public static void GetFileNames (List<string> filenames, string path)
        {
            DirectoryInfo[] dirs = null;
            DirectoryInfo dir_info = null;

            try {
                dir_info = new DirectoryInfo (path);
                dirs = dir_info.GetDirectories ();
            } catch {
                dirs = new DirectoryInfo[0];
            }

            // We don't want to import incomple-files.
            for (int i = 0; i < dirs.Length; i++) {
                if (!dirs[i].Name.Equals ("incomplete")) {
                    GetFileNames (filenames, dirs[i].FullName);
                }
            }

            FileInfo[] files = null;

            try {
                files = dir_info.GetFiles ();
                for (int i = 0; i < files.Length; i++) {
                    filenames.Add (files[i].FullName);
                }
            } catch {
            }
        }

        public static List<string> GetNewItems (List<string> previous, List<string> current)
        {
            List<string> new_items = new List<string> ();
            bool is_new;

            if (previous == null && current != null) {
                return current;
            }

            if (previous == null || current == null) {
                return null;
            }

            for (int i = 0; i < current.Count; i++) {
                is_new = true;

                for (int j = 0; j < previous.Count; j++) {
                    if (current[i] == previous[j]) {
                        is_new = false;
                        break;
                    }
                }
                if (is_new)
                    new_items.Add (current[i]);
            }

            return new_items;
        }
    }
}
