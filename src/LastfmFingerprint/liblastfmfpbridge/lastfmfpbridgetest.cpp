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

#include <gst/gst.h>

#include "gst-lastfmfpbridge.h"

gint
main (gint   argc,
      gchar *argv[])
{
  // init GStreamer
  gst_init (&argc, &argv);

  int size = 0;
  int ret = 0;


LastfmfpAudio *ma = Lastfmfp_initialize(44100, 215, 2, "", "", "", 0, 0, "");
  
int fpid = Lastfmfp_decode(ma, "/home/dufoli/Musique/music/Rock (Hard Pop)/PopRock En/hard metal/(MATRIX)  DEFTONES   my own summer .mp3", &size, &ret);

  printf("return fpid: %d ret: %d ", fpid, ret);

  ma = Lastfmfp_destroy(ma);

  return 0;
}
