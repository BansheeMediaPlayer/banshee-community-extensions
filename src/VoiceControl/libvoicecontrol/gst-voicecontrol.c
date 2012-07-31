#include <gst/gst.h>
#include <glib.h>

typedef void (*AsrResultCallback)(GstElement *sender, char *text, char *uttid);

GstPipeline *voicecontrol_init_pipeline (
	char *language_model_file,
	char *dictionary_file,
	AsrResultCallback partial_result,
	AsrResultCallback result
)
{
  GstPipeline *pipeline;
  GstElement *asr;
  GError *error = NULL;
  
  gst_init (NULL, NULL);

 pipeline = (GstPipeline *) gst_parse_launch("gconfaudiosrc ! audioconvert ! audioresample !  vader name=vad auto-threshold=true ! pocketsphinx name=asr !fakesink", &error);
  if (!pipeline) {
      if (error)
        printf ("Error building pipeline: %s", error->message);
    return NULL;
  }

  asr = gst_bin_get_by_name ((GstBin *) pipeline, "asr");
  if (language_model_file) {
    g_object_set (asr, "lm", language_model_file, NULL);
  }
  if (dictionary_file) {
    g_object_set (asr, "dict", dictionary_file, NULL);
  }
  if (partial_result) {
    g_signal_connect (asr, "partial_result", G_CALLBACK (partial_result), NULL);
  }
  g_signal_connect (asr, "result", G_CALLBACK (result), NULL);
  g_object_set (asr, "configured", TRUE, NULL);

  return pipeline;
}

void voicecontrol_start_listening (GstPipeline *pipeline)
{
  gst_element_set_state ((GstElement *)pipeline, GST_STATE_PLAYING);
}
