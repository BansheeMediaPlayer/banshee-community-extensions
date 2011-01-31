ASSEMBLY = Banshee.LiveRadio
LINK = $(BANSHEE_LIBS)

SOURCES =  \
	Banshee.LiveRadio/Genre.cs \
	Banshee.LiveRadio/GenreListModel.cs \
	Banshee.LiveRadio/LiveRadioConfigDialog.cs \
	Banshee.LiveRadio/LiveRadioFilterView.cs \
	Banshee.LiveRadio/LiveRadioPluginManager.cs \
	Banshee.LiveRadio/LiveRadioPlugins/ILiveRadioPlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/Live365Plugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/LiveRadioBasePlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/LiveRadioPluginConfigurationWidget.cs \
	Banshee.LiveRadio/LiveRadioPlugins/LiveRadioPluginError.cs \
	Banshee.LiveRadio/LiveRadioPlugins/LiveRadioPluginListModel.cs \
	Banshee.LiveRadio/LiveRadioPlugins/MagnatunePlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/RealRadiosPlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/ShoutCastPlugin.cs \
	Banshee.LiveRadio/LiveRadioPlugins/SingleSelection.cs \
	Banshee.LiveRadio/LiveRadioPlugins/XiphOrgPlugin.cs \
	Banshee.LiveRadio/LiveRadioPluginSource.cs \
	Banshee.LiveRadio/LiveRadioPluginSourceContents.cs \
	Banshee.LiveRadio/LiveRadioSource.cs \
	Banshee.LiveRadio/LiveRadioSourceContents.cs \
	Banshee.LiveRadio/LiveRadioStatistic.cs \
	Banshee.LiveRadio/LiveRadioStatisticListModel.cs

RESOURCES =  \
	LiveRadio.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/GlobalUI.xml \
	Resources/LiveRadioIcon.svg

if ENABLE_LIVERADIO
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
