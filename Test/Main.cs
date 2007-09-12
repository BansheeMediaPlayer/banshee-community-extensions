/*
 * Mirage - High Performance Music Similarity and Automatic Playlist Generator
 * http://hop.at/mirage
 * 
 * Copyright (C) 2007 Dominik Schnitzer <dominik@schnitzer.at>
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

using Mirage;

class MainClass
{
    [DllImport("libmirageaudio")]
    static extern void mirageaudio_initgst();

	private static void Test()
	{
        mirageaudio_initgst();
        Scms song1 = null;
        Scms song2 = null;
        for (int i = 0; i < 1000; i++) {
            Mirage.Mir.Analyze("/media/MUSIC/magnatune/world/yakshi/yakshi/1-sierra.mp3");
            song1 = Mirage.Mir.Analyze("/media/MUSIC/smalleval/Pop/Britney Spears - Crazy.mp3");
            song2 = Mirage.Mir.Analyze("/media/MUSIC/smalleval/Pop/Britney Spears - Lucky.mp3");
		    Mirage.Mir.Analyze("/media/MUSIC/smalleval/Eurodance/Doki Doki - Too Fast For Love.mp3");
        }
		
		Console.WriteLine("Distance = " + song1.Distance(song2));
		
		Timer t = new Timer();
		t.Start();
		int runs = 100000;
		for (int i = 0; i < runs; i++) {
			song1.Distance(song2);
		}
		long l = t.Stop();
		Dbg.WriteLine("Distance Computation: " + runs + " times - " + l + "ms; " +
			(double)l/(double)runs + "ms per comparison");
	}
	

	public static void Main(string[] args)
	{
		Test();
	}
	
}
