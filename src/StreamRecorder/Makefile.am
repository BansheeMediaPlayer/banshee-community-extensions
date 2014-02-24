ASSEMBLY = Banshee.Streamrecorder
LINK = $(BANSHEE_LIBS)
GMCS_FLAGS = -unsafe

SOURCES =  \
	Banshee.Streamrecorder/Encoder.cs \
	Banshee.Streamrecorder/Gst/Bin.cs \
	Banshee.Streamrecorder/Gst/Bus.cs \
	Banshee.Streamrecorder/Gst/BusFunc.cs \
	Banshee.Streamrecorder/Gst/Element.cs \
	Banshee.Streamrecorder/Gst/ElementFactory.cs \
	Banshee.Streamrecorder/Gst/FileSink.cs \
	Banshee.Streamrecorder/Gst/Format.cs \
	Banshee.Streamrecorder/Gst/GhostPad.cs \
	Banshee.Streamrecorder/Gst/GstObject.cs \
	Banshee.Streamrecorder/Gst/Iterator.cs \
	Banshee.Streamrecorder/Gst/Marshaller.cs \
	Banshee.Streamrecorder/Gst/Pad.cs \
	Banshee.Streamrecorder/Gst/PadBlockCallback.cs \
	Banshee.Streamrecorder/Gst/PadDirection.cs \
	Banshee.Streamrecorder/Gst/Parse.cs \
	Banshee.Streamrecorder/Gst/Pipeline.cs \
	Banshee.Streamrecorder/Gst/State.cs \
	Banshee.Streamrecorder/Gst/StateChangeReturn.cs \
	Banshee.Streamrecorder/Gst/TagList.cs \
	Banshee.Streamrecorder/Gst/TagMergeMode.cs \
	Banshee.Streamrecorder/Gst/TagSetter.cs \
	Banshee.Streamrecorder/PlayerAudioTee.cs \
	Banshee.Streamrecorder/Recorder.cs \
	Banshee.Streamrecorder/RippedFileScanner.cs \
	Banshee.Streamrecorder/StreamrecorderConfigDialog.cs \
	Banshee.Streamrecorder/StreamrecorderService.cs 

RESOURCES =  \
	Banshee.Streamrecorder.addin.xml \
	Resources/StreamrecorderButton.xml \
	Resources/StreamrecorderMenu.xml

if ENABLE_STREAMRECORDER
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
