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

//#include <math.h>
#include <time.h>

#include <glib.h>
#include <gst/gst.h>

#include "gst-lastfmfpbridge.h"

#include "FingerprintExtractor.h"

#include <map>
#include <cstring>
#include <iostream>
#include <fstream>
#include <sstream>
#include <cctype> // for tolower
#include <algorithm>
#include <string>

// hacky!
#ifdef WIN32
#define SLASH '\\'
#else
#define SLASH '/'
#endif

struct LastfmfpAudio {

    // Cancel decoding mutex
    GMutex *decoding_mutex;

    // Gstreamer
    GstElement *pipeline;
    GstElement *audio;

    gint filerate;
    gint seconds;
    gint nchannels;
    //gint samples;
    
    fingerprint::FingerprintExtractor *extractor;
	
    //input
    short *data_in;
    size_t num_samples;
	
    //output
    const char* data_out;
    size_t data_out_size;
    
    int fpid;

    gboolean quit;
    gboolean invalidate;
};

std::string filename;
    
static const int NUM_FRAMES_CLIENT = 32; // ~= 10 secs.
const char FP_SERVER_NAME[]       = "ws.audioscrobbler.com/fingerprint/query/";
const char HTTP_POST_DATA_NAME[]  = "fpdata";

// just turn it into a string. Similar to boost::lexical_cast
template <typename T>
std::string toString(const T& val)
{
   std::ostringstream oss;
   oss << val;
   return oss.str();
}

bool plain_isspace(char c)
{
   if ( c == ' ' || 
        c == '\t' ||
        c == '\n' ||
        c == '\r' )
   {
      return true;
   }
   else
   {
      return false;
   }
}

std::string simpleTrim( const std::string& str )
{
   if ( str.empty() )
      return "";

   // left trim
   std::string::const_iterator lIt = str.begin();
   for ( ; plain_isspace(*lIt) && lIt != str.end(); ++lIt );
   if ( lIt == str.end() )
      return str;

   std::string::const_iterator rIt = str.end();
   --rIt;

   for ( ; plain_isspace(*rIt) && rIt != str.begin(); --rIt );
   ++rIt;
   
   return std::string(lIt, rIt);
}

void addEntry ( std::map<std::string, std::string>& urlParams, const std::string& key, const std::string& val )
{
   if ( key.empty() || val.empty() )
      return;
   if ( urlParams.find(key) != urlParams.end() )
      return; // do not add something that was already there

    urlParams[key] = simpleTrim(val);
}


#define SRC_BUFFERLENGTH 4096

static void
Lastfmfp_cb_newpad(GstElement *decodebin, GstPad *pad, gboolean last, LastfmfpAudio *ma)
{
    GstCaps *caps;
    GstStructure *str;
    GstPad *audiopad;

    // only link once
    audiopad = gst_element_get_pad(ma->audio, "sink");
    if (GST_PAD_IS_LINKED(audiopad)) {
        g_object_unref(audiopad);
        return;
    }

    // check media type
    caps = gst_pad_get_caps(pad);
    str = gst_caps_get_structure(caps, 0);

    if (!g_strrstr(gst_structure_get_name(str), "audio")) {
        gst_caps_unref(caps);
        gst_object_unref(audiopad);
        return;
    }
    gst_caps_unref(caps);

    // link
    gst_pad_link(pad, audiopad);
    gst_object_unref(audiopad);
}

static void FingerprintFound(LastfmfpAudio *ma)
{
	//we have the fingerprint
    
    std::pair<const char*, size_t> fpData =  ma->extractor->getFingerprint();
    ma->data_out = fpData.first;
    ma->data_out_size = fpData.second;
}

extern "C" unsigned int
Lastfmfp_getVersion (LastfmfpAudio *ma)
{
	return ma->extractor->getVersion (); 
}

static void
Lastfmfp_cb_have_data(GstElement *element, GstBuffer *buffer, GstPad *pad, LastfmfpAudio *ma)
{
    // if data continues to flow/EOS is not yet processed
    if (ma->quit)
        return;

    // exit on empty buffer
    if (buffer->size <= 0)
        return;

    ma->data_in = (short*)GST_BUFFER_DATA(buffer);
    //ma->num_samples = (size_t)(GST_BUFFER_OFFSET_END (buffer) - GST_BUFFER_OFFSET (buffer));
    ma->num_samples = (size_t)(GST_BUFFER_SIZE (buffer) / sizeof(guint16));
    
	//printf("caps: %s\n", gst_caps_to_string(GST_BUFFER_CAPS(buffer)));
	//printf(" offset : %llu size: %llu \n", (unsigned long long)GST_BUFFER_OFFSET (buffer), (unsigned long long)GST_BUFFER_OFFSET_END (buffer));
	//GST_LOG ("caps are %" GST_PTR_FORMAT, GST_BUFFER_CAPS(buffer));
    //extractor.process(const short* pPCM, size_t num_samples, bool end_of_stream = false);
    //printf("data: %d %d %d %d %d %d %d %d %d %d %d %d \n", ma->data_in[0], ma->data_in[1], ma->data_in[2], ma->data_in[3], ma->data_in[4], ma->data_in[5], ma->data_in[6], ma->data_in[7], ma->data_in[8], ma->data_in[9], ma->data_in[10], ma->data_in[11]);
    if (ma->extractor->process(ma->data_in, ma->num_samples, false))//TODO check parametters
    {
        //stop the gstreamer loop to free all and return fpid
        GstBus *bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
        GstMessage* eosmsg = gst_message_new_eos(GST_OBJECT(ma->pipeline));
        gst_bus_post(bus, eosmsg);
        g_print("libLastfmfp: EOS Message sent\n");
        gst_object_unref(bus);
        ma->quit = TRUE;
        return;

    }
    
    
    return;
}

extern "C"  LastfmfpAudio*
Lastfmfp_initialize(gint seconds)
{
    LastfmfpAudio *ma;
    
    ma = g_new0(LastfmfpAudio, 1);
    ma->seconds = seconds;
	
    // cancel decoding mutex
    ma->decoding_mutex = g_mutex_new();

    return ma;
}

void
Lastfmfp_initgstreamer(LastfmfpAudio *ma, const gchar *file)
{
    GstPad *audiopad;
    GstCaps *filter_short;
    GstCaps *filter_resample;
    GstElement *cfilt_short;
    GstElement *cfilt_resample;
    GstElement *dec;
    GstElement *src;
    GstElement *sink;
    GstElement *audioresample;
    GstElement *audioconvert;

    // Gstreamer decoder setup
    ma->pipeline = gst_pipeline_new("pipeline");
    
    filename = file;
    
    // decoder
    src = gst_element_factory_make("filesrc", "source");
    g_object_set(G_OBJECT(src), "location", file, NULL);
    dec = gst_element_factory_make("decodebin", "decoder");
    g_signal_connect(dec, "new-decoded-pad", G_CALLBACK(Lastfmfp_cb_newpad), ma);
    gst_bin_add_many(GST_BIN(ma->pipeline), src, dec, NULL);
    gst_element_link(src, dec);

    // audio conversion
    ma->audio = gst_bin_new("audio");

    audioconvert = gst_element_factory_make("audioconvert", "conv");
    filter_short = gst_caps_new_simple("audio/x-raw-int",
//         "channels", G_TYPE_INT, ma->nchannels,
         "width", G_TYPE_INT, 16, 
         "depth", G_TYPE_INT, 16, 
         "endianness", G_TYPE_INT, 1234,//BYTE_ORDER, //1234, 
         "signed", G_TYPE_BOOLEAN, TRUE, 
         NULL);
    cfilt_short = gst_element_factory_make("capsfilter", "cfilt_short");
    g_object_set(G_OBJECT(cfilt_short), "caps", filter_short, NULL);
    gst_caps_unref(filter_short);

    /*audioresample = gst_element_factory_make("audioresample", "resample");

    filter_resample =  gst_caps_new_simple("audio/x-raw-float",
          "channels", G_TYPE_INT, 1,
          NULL);
    cfilt_resample = gst_element_factory_make("capsfilter", "cfilt_resample");
    g_object_set(G_OBJECT(cfilt_resample), "caps", filter_resample, NULL);
    gst_caps_unref(filter_resample);
*/
    sink = gst_element_factory_make("fakesink", "sink");
    g_object_set(G_OBJECT(sink), "signal-handoffs", TRUE, NULL);
    g_signal_connect(sink, "handoff", G_CALLBACK(Lastfmfp_cb_have_data), ma);
    

    gst_bin_add_many(GST_BIN(ma->audio),
            audioconvert, /*audioresample,
            cfilt_resample,*/ cfilt_short,
            sink, NULL);
    gst_element_link_many(audioconvert, cfilt_short,
           /*audioresample, cfilt_resample,*/
           sink, NULL);

    audiopad = gst_element_get_pad(audioconvert, "sink");
    gst_element_add_pad(ma->audio,
            gst_ghost_pad_new("sink", audiopad));
    gst_object_unref(audiopad);

    gst_bin_add(GST_BIN(ma->pipeline), ma->audio);

    // Get sampling rate of audio file
    GstClockTime max_wait = 1 * GST_SECOND;
    if (gst_element_set_state(ma->pipeline, GST_STATE_READY) == GST_STATE_CHANGE_ASYNC) {
        gst_element_get_state(ma->pipeline, NULL, NULL, max_wait);
    }
    if (gst_element_set_state(ma->pipeline, GST_STATE_PAUSED) == GST_STATE_CHANGE_ASYNC) {
        gst_element_get_state(ma->pipeline, NULL, NULL, max_wait);
    }

    GstPad *pad = gst_element_get_pad(sink, "sink");
    GstCaps *caps = gst_pad_get_negotiated_caps(pad);
    if (GST_IS_CAPS(caps)) {
        GstStructure *str = gst_caps_get_structure(caps, 0);
        gst_structure_get_int(str, "rate", &ma->filerate);
        gst_structure_get_int(str, "channels", &ma->nchannels);
    } else {
        ma->filerate = -1;
    }
    gst_caps_unref(caps);
    gst_object_unref(pad);
}

extern "C" const char*
Lastfmfp_decode(LastfmfpAudio *ma, const gchar *file, int* size, int* ret)
{
    GstBus *bus;

    ma->quit = FALSE;

    g_mutex_lock(ma->decoding_mutex);
    ma->invalidate = FALSE;
    g_mutex_unlock(ma->decoding_mutex);

    // Gstreamer setup
    Lastfmfp_initgstreamer(ma, file);
    //lastfm setup
    ma->extractor = new fingerprint::FingerprintExtractor();
    ma->extractor->initForQuery(ma->filerate, ma->nchannels, ma->seconds);
    
    if (ma->filerate < 0) {
        *size = 0;
        *ret = -1;

        // Gstreamer cleanup
        gst_element_set_state(ma->pipeline, GST_STATE_NULL);
        gst_object_unref(GST_OBJECT(ma->pipeline));

        return NULL;
    }

    // decode...
    gst_element_set_state(ma->pipeline, GST_STATE_PLAYING);
    g_print("libLastfmfp: decoding %s\n", file);


    bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
    gboolean decoding = TRUE;
    *ret = 0;
    while (decoding) {
        GstMessage* message = gst_bus_timed_pop_filtered(bus, GST_MSECOND*100,
               (GstMessageType) (GST_MESSAGE_ERROR | GST_MESSAGE_EOS));

        if (message == NULL)
            continue;

        switch (GST_MESSAGE_TYPE(message)) {
            case GST_MESSAGE_ERROR: {
                GError *err;
                gchar *debug;

                gst_message_parse_error(message, &err, &debug);
                g_print("libLastfmfp: error: %s\n", err->message);
                g_error_free(err);
                g_free(debug);
                decoding = FALSE;
                *ret = -1;

                break;
            }
            case GST_MESSAGE_EOS: {
	            //ma->extractor->process(0, static_cast<size_t>(0), true);
            	FingerprintFound(ma);
                g_print("libLastfmfp: EOS Message received\n");
                decoding = FALSE;
                break;
            }
            default:
                break;
        }
        gst_message_unref(message);
    }
    gst_object_unref(bus);


    g_mutex_lock(ma->decoding_mutex);

    // Gstreamer cleanup
    gst_element_set_state(ma->pipeline, GST_STATE_NULL);
    gst_object_unref(GST_OBJECT(ma->pipeline));

    if (ma->invalidate) {
        //*size = 0;
        *ret = -2;
    } else {
        //*size = ma->nchannels/2 + 1;
    }

	*size = ma->data_out_size;
	
    g_mutex_unlock(ma->decoding_mutex);

    return ma->data_out;
}

extern "C" LastfmfpAudio*
Lastfmfp_destroy(LastfmfpAudio *ma)
{
    g_print("libLastfmfp: destroy.\n");

    g_mutex_free(ma->decoding_mutex);

    // common
    free(ma);

    return NULL;
}

extern "C" void
Lastfmfp_initgst()
{
    gst_init(NULL, NULL);
}

extern "C" void
Lastfmfp_canceldecode(LastfmfpAudio *ma)
{
    if (GST_IS_ELEMENT(ma->pipeline)) {

        GstState state;
        gst_element_get_state(ma->pipeline, &state, NULL, 100*GST_MSECOND);

        if (state != GST_STATE_NULL) {
            g_mutex_lock(ma->decoding_mutex);

            GstBus *bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
            GstMessage* eosmsg = gst_message_new_eos(GST_OBJECT(ma->pipeline));
            gst_bus_post(bus, eosmsg);
            g_print("libLastfmfp: EOS Message sent\n");
            gst_object_unref(bus);

            ma->invalidate = TRUE;

            g_mutex_unlock(ma->decoding_mutex);
        }
    }
}


