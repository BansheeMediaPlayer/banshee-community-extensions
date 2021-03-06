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
#include <stdio.h>

#include "gst-lastfmfpbridge.h"

gint
main (gint   argc,
      gchar *argv[])
{
  // init GStreamer
  gst_init (&argc, &argv);

  int size = 0;
  int ret = 0;

  //TODO parse argv for param of initialize
  LastfmfpAudio *ma = Lastfmfp_initialize(215);
  
  const char* fpid = Lastfmfp_decode(ma, argv[0], &size, &ret);

  printf("return fpid: %s ret: %d ", fpid, ret);

  ma = Lastfmfp_destroy(ma);

  return 0;
}
