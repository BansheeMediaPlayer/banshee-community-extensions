/*
 * AudaciousUDPStub.c: Plugin stub for Audacious that exports visualization data
 * over a UDP socket.
 *
 * Copyright (C) 2006  Chris Howie
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

#include <stdio.h>
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
static void openvp_about(void);
static void openvp_configure(void);
static void openvp_playback_start(void);
static void openvp_playback_stop(void);
static void openvp_render_freq(gint16 freq_data[2][256]);
static void openvp_render_pcm(gint16 freq_data[2][512]);

// Callback functions
VisPlugin openvp_vtable = {
	0,	// Handle, filled in by Audacious
	0,	// Filename, filled in by Audacious

	0,	// Session ID
	"OpenVP UDP stub",	// description

	2,	// # of PCM channels for render_pcm()
	2,	// # of freq channels wanted for render_freq()

	openvp_init,		// Called when plugin is enabled
	openvp_cleanup,	// Called when plugin is disabled
	openvp_about,		// Show the about box
	openvp_configure,	// Show the configure box
	0,		// Called to disable plugin, filled in by Audacious
	openvp_playback_start,// Called when playback starts
	openvp_playback_stop,	// Called when playback stops
	openvp_render_pcm,		// Render the PCM data, must return quickly
	openvp_render_freq	// Render the freq data, must return quickly
};

// Audacious entry point
VisPlugin *get_vplugin_info() {
	return &openvp_vtable;
}

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

static void openvp_about() {
}

static void openvp_configure() {
}

static void openvp_playback_start() {
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
	
	// Frequency data is always the last.
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
