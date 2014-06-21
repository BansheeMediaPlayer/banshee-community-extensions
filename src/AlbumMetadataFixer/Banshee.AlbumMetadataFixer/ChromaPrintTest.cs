using System;
using System.Collections.Generic;
using Gst;

namespace AlbumMetadataFixer
{
    class Recording
    {
        public string ID
        { private set; get; }

        public string Title
        { private set; get; }

        public List<string> Artists
        { private set; get; }

        public List<string> ReleaseGroups
        { private set; get; }

        public Recording(string id, string title, List<string> artists, List<string> release_groups) {
            ID = id;
            Title = title;
            Artists = artists;
            ReleaseGroups = release_groups;
        }
    }

    class AcoustIDReader
    {
        string fingerprint = null;
        long duration = -1;
        string filename;
        string current_id;
        string key;
        Pipeline pipeline;
        private List<Recording> recordings;

        Action<string, List<Recording>> completion_handler;

        public AcoustIDReader (string key) {
            this.key = key;
        }

        public void GetID (string filename, Action<string, List<Recording>> completion_handler) {
            this.filename = filename;
            this.completion_handler = completion_handler;
            StartPipeline ();
        }

        public void StartPipeline () {
            pipeline = new Pipeline ();

            Element src = ElementFactory.Make ("filesrc", "source"),
            decoder = ElementFactory.Make ("decodebin", "decoder"),
            chroma_print = ElementFactory.Make ("chromaprint", "processor"),
            sink = ElementFactory.Make ("fakesink", "sink");

            pipeline.Bus.AddSignalWatch();
            pipeline.Bus.Message += MsgHandler;

            if (src == null || decoder == null || chroma_print == null || sink == null) {
                Console.WriteLine ("cannot find necessary gstreamer element (filesrc, decodebin, chromaprint or fakesink)!");
                pipeline = null;
                return;
            }

            sink ["sync"] = 0;
            src ["location"] = filename;

            pipeline.Add (src);
            pipeline.Add (decoder);
            pipeline.Add (chroma_print);
            pipeline.Add (sink);

            src.Link (decoder);
            chroma_print.Link (sink);

            decoder.PadAdded += (o, args) => {
                args.NewPad.Link (chroma_print.GetStaticPad ("sink"));
            };

            pipeline.SetState (State.Playing);
        }

        private void ReadID () {
            current_id = string.Empty;

            if (fingerprint == null || duration == -1) {
                // todo: timeout or sth?
                return;
            }

            string url = string.Format ("http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=xml&client={0}&duration={1}&fingerprint={2}", key, duration, fingerprint);
                                                
            var reader = new System.Xml.XmlTextReader (url);
            //var reader = new System.Xml.XmlTextReader ("/home/loganek/lookup");
            var doc = new System.Xml.XmlDocument ();
            doc.Load (reader);

            System.Xml.XmlNode status = doc.SelectSingleNode ("/response/status");

            if (status == null) {
                OnCompleted ();
            }

            string response_status = status.InnerText;

            if (response_status != "ok") {
                OnCompleted ();
            }

            FindBestID (doc.SelectNodes ("/response/results/result"));
            OnCompleted ();
        }

        private void FindBestID (System.Xml.XmlNodeList results) {
            double current_score = 0;

            foreach (System.Xml.XmlNode result in results) {
                double score;

                if (result ["score"] == null || result ["id"] == null || !double.TryParse (result ["score"].InnerText, out score)) {
                    continue;
                }

                if (score > current_score) {
                    current_score = score;
                    current_id = result ["id"].InnerText;

                    ProcessRecordings (result);
                }
            }
        }

        private void ProcessRecordings (System.Xml.XmlNode result) {
            recordings = new List<Recording>();
            foreach (System.Xml.XmlNode recording in result.SelectNodes ("recordings/recording")) {
                if (recording ["title"] != null && recording ["id"] != null) {
                    recordings.Add (new Recording (recording ["id"].InnerText, recording ["title"].InnerText, ReadArtists (recording), ReadReleaseGroups (recording)));
                }
            }
        }

        private List<string> ReadArtists (System.Xml.XmlNode result) {
            var list = new List<string> ();
            foreach (System.Xml.XmlNode artist in result.SelectNodes ("artists/artist")) {
                if (artist ["name"] != null) {
                    list.Add (artist ["name"].InnerText);
                }
            }
            return list;
        }

        private List<string> ReadReleaseGroups (System.Xml.XmlNode result) {
            var list = new List<string> ();
            foreach (System.Xml.XmlNode releasegroup in result.SelectNodes ("releasegroups/releasegroup")) {
                if (releasegroup ["title"] != null) {
                    list.Add (releasegroup ["title"].InnerText);
                }
            }
            return list;
        }

        private void MsgHandler(object o, MessageArgs args) {
            switch (args.Message.Type) {
                case MessageType.DurationChanged:
                if (pipeline.QueryDuration (Format.Time, out duration)) {
                    duration /= Gst.Constants.SECOND;
                    ReadID ();
                }
                break;
                case MessageType.Eos:
                // todo: finish
                break;
                case MessageType.Tag:
                TagList tags = args.Message.ParseTag();
                tags.GetString("chromaprint-fingerprint", out fingerprint);

                if (fingerprint != null) {
                    ReadID ();
                }
                break;
            }
        }

        private void OnCompleted ()
        {
            if (completion_handler != null) {
                completion_handler (current_id, recordings);
            }
        }
    }

    class ChromaPrintTest
    {
        static GLib.MainLoop Loop;

        public static void Main(string[] argv)
        {
            if (argv.Length < 1) {
                Console.WriteLine ("Usage: ChromaPrintTest.exe <audio file>");
                return;
            }

            Application.Init ();
            Loop = new GLib.MainLoop();

            var reader = new AcoustIDReader ("8XaBELgH"); // todo: it's example key. Banshee should be registered in acoustID system.
            reader.GetID (argv [0], (id, list) => {
                Console.WriteLine ("Track ID: " + id);
                foreach (Recording rec in list) {
                    Console.WriteLine ("=========================");
                    Console.WriteLine ("Recording ID: " + rec.ID);
                    Console.WriteLine ("Title: " + rec.Title);
                    Console.WriteLine ("Artists: ");
                    foreach (string artist in rec.Artists) {
                        Console.WriteLine ("\t * " + artist);
                    }
                    Console.WriteLine("Release Groups: ");
                    foreach (string release_group in rec.ReleaseGroups) {
                        Console.WriteLine ("\t * " + release_group);
                    }
                }

                Loop.Quit ();
            });

            Loop.Run();
        }
    }
}
