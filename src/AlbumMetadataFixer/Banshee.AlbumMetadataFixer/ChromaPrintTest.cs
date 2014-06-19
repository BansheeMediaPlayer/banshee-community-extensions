using System;
using Gst;

namespace AlbumMetadataFixer
{
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

            Pipeline pipeline = new Pipeline ();

            Element src = ElementFactory.Make ("filesrc", "source"),
            decoder = ElementFactory.Make ("decodebin", "decoder"),
            chroma_print = ElementFactory.Make ("chromaprint", "processor"),
            sink = ElementFactory.Make ("fakesink", "sink");

            pipeline.Bus.AddSignalWatch();
            pipeline.Bus.Message += (o, args) =>
            {
                switch (args.Message.Type) {
                case MessageType.Eos:
                    Loop.Quit();
                    break;
                case MessageType.Tag:
                    TagList tags = args.Message.ParseTag();
                    string fingerprint;
                    tags.GetString("chromaprint-fingerprint", out fingerprint);

                    if (fingerprint != null)
                        Console.WriteLine("Fingerprint: " + fingerprint);

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
