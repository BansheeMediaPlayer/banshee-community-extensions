ASSEMBLY = Banshee.LiveRadio
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.LiveRadio/GenreListModel.cs \
	Banshee.LiveRadio/LiveRadioFilterView.cs \
	Banshee.LiveRadio/LiveRadioPluginManager.cs \
	Banshee.LiveRadio/LiveRadioPlugins/Genre.cs \
	Banshee.LiveRadio/LiveRadioPlugins/ILiveRadioPlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/Live365Plugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/LiveRadioBasePlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/ShoutCastPlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/XiphOrgPlugin.cs \
	Banshee.LiveRadio/LiveRadioPluginSource.cs \
	Banshee.LiveRadio/LiveRadioPluginSourceContents.cs \
	Banshee.LiveRadio/LiveRadioSource.cs

RESOURCES =  \
	LiveRadio.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml

if ENABLE_LIVERADIO
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
