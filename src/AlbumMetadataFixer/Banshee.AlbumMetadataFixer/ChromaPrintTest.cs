using System;
using Gst;

namespace AlbumMetadataFixer
{
    class ChromaPrintTest
    {
        static GLib.MainLoop Loop;
        static long duration = -1;
        static string fingerprint = null;

        static string ReadID ()
        {            
            string current_id = string.Empty;

            if (fingerprint == null || duration == -1) {
                Console.WriteLine ("Fingerprint or duration unavialable yet");
                return current_id;
            }

            string key = "8XaBELgH"; // todo: it's example key. Banshee should be registered in acoustID system.
            string url = string.Format ("http://api.acoustid.org/v2/lookup?format=xml&client={0}&duration={1}&fingerprint={2}", key, duration, fingerprint);

            var reader = new System.Xml.XmlTextReader (url);
            var doc = new System.Xml.XmlDocument ();
            doc.Load (reader);

            System.Xml.XmlNode status = doc.SelectSingleNode ("/response/status");

            if (status == null) {
                Console.WriteLine ("Cannot read response's status");
                return current_id;
            }

            string response_status = status.InnerText;

            if (response_status != "ok") {
                Console.WriteLine ("Invalid response status. Expected 'ok', but is: `{0}`", response_status);
                return current_id;
            }

            System.Xml.XmlNodeList results = doc.SelectNodes ("/response/results/result");

            double current_score = 0;

            foreach (System.Xml.XmlNode result in results) {
                double score;

                if (result ["score"] == null || result ["id"] == null || !double.TryParse (result ["score"].InnerText, out score)) {
                    continue;
                }

                if (score > current_score) {
                    current_score = score;
                    current_id = result ["id"].InnerText;
                }
            }

            return current_id;
        }

        public static void Main(string[] argv)
        {
            if (argv.Length < 1) {
                Console.WriteLine ("Usage: ChromaPrintTest.exe <audio file>");
                return;
            }

            Application.Init ();
            Loop = new GLib.MainLoop();

            Pipeline pipeline = new Pipeline ();

            Element src = ElementFactory.Make ("filesrc", "source"),
            decoder = ElementFactory.Make ("decodebin", "decoder"),
            chroma_print = ElementFactory.Make ("chromaprint", "processor"),
            sink = ElementFactory.Make ("fakesink", "sink");

            pipeline.Bus.AddSignalWatch();
            pipeline.Bus.Message += (o, args) =>
            {
                switch (args.Message.Type) {
                case MessageType.DurationChanged:
                    bool ok = pipeline.QueryDuration (Format.Time, out duration);
                    if (ok) {
                        duration /= Gst.Constants.SECOND;
                        Console.WriteLine ("Duration: {0}", duration);
                        ReadID ();
                    }
                    break;
                case MessageType.Eos:
                    Loop.Quit();
                    break;
                case MessageType.Tag:
                    TagList tags = args.Message.ParseTag();
                    tags.GetString("chromaprint-fingerprint", out fingerprint);

                    if (fingerprint != null) {
                        Console.WriteLine("Fingerprint: " + fingerprint);
                        ReadID ();
                    }
                    break;
                }
            };

            if (src == null || decoder == null || chroma_print == null || sink == null) {
                Console.WriteLine ("cannot find necessary gstreamer element (filesrc, decodebin, chromaprint or fakesink)!");
                return;
            }

            sink ["sync"] = 0;
            src ["location"] = argv[0];

            pipeline.Add (src);
            pipeline.Add (decoder);
            pipeline.Add (chroma_print);
            pipeline.Add (sink);

            src.Link (decoder);
            chroma_print.Link (sink);
            chroma_print.AddNotification ((o, arg) => {
                // todo: Why it doesn't work? Probably some kind of a bug in chromaprint element?
                // todo: check it.
                // todo: checked: there is no `notify` on property change
                var e = o as Element;
                if (e == null || e.Name != "processor" || arg.Property != "fingerprint") {
                    return;
                }

                Console.WriteLine ("FingerPrint: " + chroma_print ["fingerprint"] ?? "null value");
            });

            chroma_print ["duration"] = 60;

            decoder.PadAdded += (o, args) => {
                args.NewPad.Link (chroma_print.GetStaticPad ("sink"));
            };

            pipeline.SetState (State.Playing);

            Loop.Run();
        }
    }
}
