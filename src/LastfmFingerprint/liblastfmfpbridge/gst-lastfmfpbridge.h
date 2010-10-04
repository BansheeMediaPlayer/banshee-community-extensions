/*
 * last.fm Fingerprint - Accoustic fingerprint lib to get puid of song
 * inspired from mirage : http://hop.at/mirage
 * 
 * Copyright (C) 2010 Olivier Dufour <olivier(dot)duff(at)gmail(dot)com
 * Modified version of Dominik Schnitzer <dominik@schnitzer.at>
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

#include <glib.h>

typedef struct LastfmfpAudio LastfmfpAudio;

extern "C" LastfmfpAudio*
Lastfmfp_initialize(gint rate, gint seconds, gint winsize, const gchar *artist, const gchar *album, const gchar *title, gint tracknum, gint year, const gchar *genre);

extern "C" int
Lastfmfp_decode(LastfmfpAudio *ma, const gchar *file, int* size, int* ret);

extern "C" LastfmfpAudio*
Lastfmfp_destroy(LastfmfpAudio *ma);

extern "C" void
Lastfmfp_canceldecode(LastfmfpAudio *ma);

extern "C" void
Lastfmfp_initgst();
