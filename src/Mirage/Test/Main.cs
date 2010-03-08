/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007-2008 Dominik Schnitzer <dominik@schnitzer.at>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Data;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection.Indexer;

using Mirage;

class MainClass
{
    [DllImport("libmirageaudio")]
    static extern void mirageaudio_initgst();

    private static void Test()
    {
        mirageaudio_initgst();

        string song1_filename = "/home/lorentz/Music/Library/Pachelbel/Johann Pachelbel - Canon And Gigue In D Major For 3 Violins And Basso Continuo.mp3";
        string song2_filename = "/home/lorentz/Music/Library/Karajan Adagios/CD 1/Pachelbel - Canon in d Major (Kanon And Gigue in d Major = d Dur) av Johann Pachelbel.mp3";
        Scms song1 = null;
        Scms song2 = null;
        
        DbgTimer t1 = new DbgTimer();
        t1.Start();
        int runs = 10;
        for (int i = 0; i < runs; i++) {
            Analyzer.Analyze(song1_filename);
        }
        long l1 = 0;
        t1.Stop(ref l1);
        Dbg.WriteLine("Analysis: " + runs + " times - " + l1 + "ms; " +
                      (double)l1/(double)runs + "ms per analysis");

        song1 = Analyzer.Analyze(song1_filename);
        song2 = Analyzer.Analyze(song2_filename);
        
        ScmsConfiguration config = new ScmsConfiguration (Analyzer.MFCC_COEFFICIENTS);

        Console.WriteLine("Distance = " + Scms.Distance (song1, song2, config));
        
        DbgTimer t2 = new DbgTimer();
        t2.Start();
        runs = 100000;
        for (int i = 0; i < runs; i++) {
            Scms.Distance (song1, song2, config);
        }
        long l2 = 0;
        t2.Stop(ref l2);
        Dbg.WriteLine("Distance Computation: " + runs + " times - " + l2 + "ms; " +
                      (double)l2/(double)runs + "ms per comparison");
    }

/*	private static void GenreClassification()
	{
		// Position of the Genre of the filename seperated by '/' 
		int genrePos = 5;
		
		// Location of the mirage database
		string mirageDb = "/home/aeneas/.cache/banshee-mirage/mirage.db";
		
		ThreadAssist.InitializeMainThread ();
		
		ServiceManager.Initialize ();
		ServiceManager.RegisterService<DBusServiceManager> ();
		ServiceManager.RegisterService<BansheeDbConnection> ();
		ServiceManager.RegisterService<SourceManager> ();
		ServiceManager.RegisterService<CollectionIndexerService> ();
		ServiceManager.Run ();
		
		String query = String.Format(@"SELECT TrackID, Uri FROM CoreTracks");
		ServiceManager.DbConnection.Query(query);
		
		IDataReader reader = ServiceManager.DbConnection.Query(query);
		
		Dictionary<int, string> ht = new Dictionary<int,string>();
		
		while (reader.Read()) {
			int trackId = Convert.ToInt32(reader["TrackID"]);
			Uri uri = new Uri((string)reader["Uri"]);
			
			string filename = Uri.UnescapeDataString(uri.PathAndQuery);  
			ht[trackId] = filename;
		}
		
		Db db = new Db(mirageDb);
		
		int hit = 0;
			
		foreach (int trackId in ht.Keys) {
			string[] s = ht[trackId].Split('/');
			string seedGenre = s[genrePos];
			
			int[] seeds = new int[1];
			seeds[0] = trackId;
			int[] exclude = new int[1];
			exclude[0] = trackId;
			try {
				int[] similar = Mirage.Mir.SimilarTracks(seeds, exclude, db, 1);
				System.Console.Out.WriteLine("SEED: TrackId: {0}, Genre: {1}", trackId, seedGenre);
				
				int simId = similar[0];
				string simFile = ht[simId];
				string[] s2 = simFile.Split('/');
				string simGenre = s2[genrePos];
				
				if (simGenre.Equals(seedGenre)) {
					hit++;
				}
				
				System.Console.Out.WriteLine("SIMILAR: trackID: {0}, Genre: {1}", simId, simGenre);
			} catch (DbTrackNotFoundException) {
				System.Console.Out.WriteLine("track Not found");
			}
		}
		
		Console.Out.WriteLine("Genre Classification Accuracy: {0}%", ((double)hit/(double)ht.Count)*100);
		
	}
*/


	public static void Main(string[] args)
	{
		Test();
//		GenreClassification();
	}
	
}
