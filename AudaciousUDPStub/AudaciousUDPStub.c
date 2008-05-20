/*
 * AudaciousUDPStub.c: Plugin stub for Audacious that exports visualization data
 * over a UDP socket.
 *
 * Copyright (C) 2006-2008  Chris Howie
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA 
 */

#include <stdio.h>
#include <stdlib.h>
#include <audacious/plugin.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>

#ifndef NULL
# define NULL ((void *) 0)
#endif

static void openvp_init(void);
static void openvp_cleanup(void);
static void openvp_playback_stop(void);
static void openvp_render_freq(gint16 freq_data[2][256]);
static void openvp_render_pcm(gint16 freq_data[2][512]);

// Callback functions
static VisPlugin openvp_vtable = {
	.description = "OpenVP UDP stub",

	.num_pcm_chs_wanted = 2,
	.num_freq_chs_wanted = 2,

	.init = openvp_init,
	.cleanup = openvp_cleanup,
	.playback_stop = openvp_playback_stop,
	.render_pcm = openvp_render_pcm,
	.render_freq = openvp_render_freq
};

static VisPlugin *openvp_list[] = { &openvp_vtable, NULL };

SIMPLE_VISUAL_PLUGIN(openvp, openvp_list);

static int socket_ = -1;
static struct sockaddr_in destAddr_;

union {
	gint16 pcm[2][512];
	gint16 freq[2][256];
} zero_vis;

typedef enum {
	PositionUpdate = 1,
	TitleUpdate = 2,
	
	PCMUpdate = 3,
	SpectrumUpdate = 4,
	
	SliceComplete = 5,
} MessageType;

static void openvp_init() {
	struct sockaddr_in addr;
	struct hostent *hp;
	char* host = "127.0.0.1";

	memset(zero_vis.pcm, 0, sizeof(zero_vis.pcm));

	socket_ = socket(AF_INET, SOCK_DGRAM, 0);
	if (socket_ == -1) {
		fprintf(stderr,"ERROR: openvp_init failed to create a socket\n");
		perror("socket");
		return;
	}
	memset(&addr, 0, sizeof(addr));
	addr.sin_addr.s_addr = INADDR_ANY;
	addr.sin_family = PF_INET;
	addr.sin_port = htons(0);
	if (bind(socket_, (struct sockaddr *) &addr, sizeof(addr)) < 0) {
		fprintf(stderr,"ERROR: openvp_init failed to bind to port %d\n", 0);
		perror("bind");
		socket_ = -1;
		return;
	}
	if (!(hp = gethostbyname(host))) {
		fprintf(stderr,"ERROR: openvp_init::setDestination failed "
			"to get address for \"%s\"\n", host);
		perror("gethostbyname");
		return;
	}
	memset(&destAddr_, 0, sizeof(destAddr_));
	memcpy(&destAddr_.sin_addr, hp->h_addr, hp->h_length);
	destAddr_.sin_family = hp->h_addrtype;
	destAddr_.sin_port = htons(40507);
}

static void openvp_zero_vis() {
	openvp_render_pcm(zero_vis.pcm);
	openvp_render_freq(zero_vis.freq);
}

static void openvp_cleanup() {
	if (socket_ != -1) {
		openvp_zero_vis();
		close(socket_);
		socket_ = -1;
	}
}

static void openvp_playback_stop() {
	openvp_zero_vis();
}

static void openvp_short_to_float(gint16 *in, float *out, int count) {
	while (count--)
		*(out++) = (float) *(in++) / 32768.0f;
}

static void openvp_send_slice_complete() {
	MessageType type = SliceComplete;
	
	if (socket_ == -1)
		return;
	
	sendto(socket_, (void *) &type, sizeof(type), 0,
		(struct sockaddr *)&destAddr_, sizeof(destAddr_));
}

static void openvp_send_position() {
	struct {
		MessageType type;
		float position;
	} out;
	
	Playlist *playlist;
	int pos;
	
	if (socket_ == -1)
		return;
	
	playlist = aud_playlist_get_active();
	
	pos = aud_playlist_get_position(playlist);
	
	out.type = PositionUpdate;
	out.position = (float) audacious_drct_get_time() / 1000;
	
	sendto(socket_, (void *) &out, sizeof(out), 0,
		(struct sockaddr *) &destAddr_, sizeof(destAddr_));
}

static char *openvp_last_title = NULL;

static void openvp_send_title() {
	struct {
		MessageType type;
		char str;
	} *out;
	
	int pos;
	int outlen;
	char *title;
	Playlist *playlist;
	
	if (socket_ == -1)
		return;
	
	playlist = aud_playlist_get_active();
	
	pos = aud_playlist_get_position(playlist);
	
	if (pos == -1) {
		title = "";
	} else {
		title = aud_playlist_get_songtitle(playlist, pos);
		
		if (title == NULL)
			title = "";
	}
	
	if (openvp_last_title != NULL) {
		if (strcmp(title, openvp_last_title) == 0)
			return;
		
		free(openvp_last_title);
	}
	
	openvp_last_title = strdup(title);
	
	outlen = 4 + strlen(title);
	
	out = malloc(outlen);
	
	out->type = TitleUpdate;
	memcpy(&out->str, title, strlen(title));
	
	sendto(socket_, (void *) out, outlen, 0,
		(struct sockaddr *) &destAddr_, sizeof(destAddr_));
	
	free(out);
}

static void openvp_render_freq(gint16 freq_data[2][256]) {
	struct {
		MessageType type;
		float data[2][256];
	} out;

	if (socket_ == -1)
		return;

	out.type = SpectrumUpdate;
	
	openvp_short_to_float((gint16 *) freq_data, (float *) out.data, 2 * 256);

	sendto(socket_, (void *) &out, sizeof(out), 0,
		(struct sockaddr *) &destAddr_, sizeof(destAddr_));
	
	// Frequency data is always the last.  Send the rest and signal completion.
	openvp_send_position();
	openvp_send_title();
	openvp_send_slice_complete();
}

static void openvp_render_pcm(gint16 pcm_data[2][512]) {
	struct {
		MessageType type;
		float data[2][512];
	} out;

	if (socket_ == -1)
		return;

	out.type = PCMUpdate;
	
	openvp_short_to_float((gint16 *) pcm_data, (float *) out.data, 2 * 512);

	sendto(socket_, (void *) &out, sizeof(out), 0,
		(struct sockaddr *) &destAddr_, sizeof(destAddr_));
}
