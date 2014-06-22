using System;
using System.Collections.Generic;
using System.Xml;
using Gst;

namespace AlbumMetadataFixer
{
	#region Helper classes
	class ReleaseGroup
	{
		public string ID
		{ private set; get; }

		public string Title
		{ private set; get; }

		public string Type
		{ private set; get; }

		public List<string> SecondaryTypes
		{ private set; get; }

		public ReleaseGroup (string id, string title, string type, List<string> secondary_types) {
			ID = id;
			Title = title;
			Type = type;
			SecondaryTypes = secondary_types;
		}
	}

	class Artist
	{
		public string ID
		{ private set; get; }

		public string Name
		{ private set; get; }

		public Artist (string id, string name) {
			ID = id;
			Name = name;
		}
	}

	class Recording
	{
		public string ID
		{ private set; get; }

		public string Title
		{ private set; get; }

		public List<Artist> Artists
		{ private set; get; }

		public List<ReleaseGroup> ReleaseGroups
		{ private set; get; }

		public Recording(string id, string title, List<Artist> artists, List<ReleaseGroup> release_groups) {
			ID = id;
			Title = title;
			Artists = artists;
			ReleaseGroups = release_groups;
		}
	}
	#endregion Helper classes

	class AcoustIDReader
	{
		private string fingerprint;
		private long duration = -1;
		private string filename;
		private string key;
		private Pipeline pipeline;
		private Action<string, List<Recording>> completionHandler;

		public AcoustIDReader (string key) {
			this.key = key;
		}

		public void GetID (string filename, Action<string, List<Recording>> completion_handler) {
			this.filename = filename;
			completionHandler = completion_handler;
			StartPipeline ();
		}

		private void StartPipeline () {
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

		private void ReadID () {

			if (fingerprint == null || duration == -1) {
				// todo: timeout or sth?
				return;
			}

			string url = string.Format ("http://api.acoustid.org/v2/lookup?meta=recordings+releasegroups&format=xml&client={0}&duration={1}&fingerprint={2}", key, duration, fingerprint);
			var xmlReader = new XmlAcoustIDReader (url, completionHandler);
			xmlReader.ReadID ();
		}
	}

	class XmlAcoustIDReader
	{
		private string current_id;
		private string url;
		private List<Recording> recordings;
		private Action<string, List<Recording>> completionHandler;

		public XmlAcoustIDReader (string url, Action<string, List<Recording>> completion_handler) {
			this.url = url;
			this.completionHandler = completion_handler;
		}

		public void ReadID () {
			var reader = new XmlTextReader (url);
			var doc = new XmlDocument ();

			current_id = string.Empty;
			doc.Load (reader);
			XmlNode status = doc.SelectSingleNode ("/response/status");

			if (status == null) {
				OnCompleted ();
			}

			if (status.InnerText != "ok") {
				OnCompleted ();
			}

			FindBestID (doc.SelectNodes ("/response/results/result"));
			OnCompleted ();
		}

		private void FindBestID (XmlNodeList results) {
			double current_score = 0;
			foreach (XmlNode result in results) {
				double score;

				if (result ["score"] == null || result ["id"] == null || !double.TryParse (result ["score"].InnerText, out score)) {
					continue;
				}

				if (score > current_score) {
					current_score = score;
					current_id = result ["id"].InnerText;
					ReadRecordings (result);
				}
			}
		}

		private void ReadRecordings (XmlNode result) {
			recordings = new List<Recording>();
			foreach (XmlNode recording in result.SelectNodes ("recordings/recording")) {
				if (recording ["id"] != null) {
					recordings.Add (new Recording (
						recording ["id"].InnerText, 
						GetInner (recording ["title"]), 
						ReadArtists (recording), 
						ReadReleaseGroups (recording)
						));
				}
			}
		}

		private void OnCompleted ()
		{
			if (completionHandler != null) {
				completionHandler (current_id, recordings);
			}
		}

		private static List<ReleaseGroup> ReadReleaseGroups (XmlNode result)
		{
			var list = new List<ReleaseGroup> ();
			foreach (XmlNode releasegroup in result.SelectNodes ("releasegroups/releasegroup")) {
				if (releasegroup ["id"] != null) {
					var secondary_types = new List<string> ();
					foreach (XmlNode sec_type in releasegroup.SelectNodes ("secondarytypes/secondarytype")) {
						secondary_types.Add (GetInner (sec_type));
					}
					list.Add (new ReleaseGroup (
						releasegroup ["id"].InnerText, 
						GetInner (releasegroup ["title"]), 
						GetInner (releasegroup ["type"]),
						secondary_types));
				}
			}
			return list;
		}

		private static List<Artist> ReadArtists (XmlNode result)
		{
			var list = new List<Artist> ();
			foreach (XmlNode artist in result.SelectNodes ("artists/artist")) {
				if (artist ["name"] != null) {
					list.Add (new Artist (artist ["id"].InnerText, GetInner (artist ["name"])));
				}
			}
			return list;
		}

		private static string GetInner(XmlNode node) {
			return node == null ? "" : node.InnerText;
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

			var reader = new AcoustIDReader ("TP95csTg");
			reader.GetID (argv [0], (id, list) => {
				Console.WriteLine ("Track ID: " + id);
				foreach (Recording rec in list) {
					Console.WriteLine ("=========================");
					Console.WriteLine ("Recording ID: " + rec.ID);
					Console.WriteLine ("Title: " + rec.Title);
					Console.WriteLine ("Artists: ");
					foreach (Artist artist in rec.Artists) {
						Console.WriteLine ("\t * {0} (ID: {1})", artist.Name, artist.ID);
					}
					Console.WriteLine("Release Groups: ");
					foreach (ReleaseGroup release_group in rec.ReleaseGroups) {
						string sec_types = "";
						if (release_group.SecondaryTypes.Count == 0) {
							sec_types = "no secondary types";
						} else {
							foreach (string t in release_group.SecondaryTypes) {
								sec_types += t + ", ";
							}
							sec_types = sec_types.Remove (sec_types.Length - 2);
						}
						Console.WriteLine ("\t * {0} (Type: {1} /{3}/, ID: {2})", release_group.Title, release_group.Type, release_group.ID, sec_types);
					}
				}

				Loop.Quit ();
			});

			Loop.Run();
		}
	}
}
