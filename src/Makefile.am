ASSEMBLY = Alarm.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-alarm.schemas.in

SOURCES = \
	$(srcdir)/AlarmClockService.cs \
	$(srcdir)/VolumeFade.cs \
	$(srcdir)/Alarm.cs \
	$(srcdir)/AlarmConfigDialog.cs \
	$(srcdir)/GConfSchemas.cs \
	$(srcdir)/SleepTimerConfigDialog.cs \
	$(srcdir)/ConfigurationWidget.cs \
	$(srcdir)/AssemblyInfo.cs

RESOURCES = Banshee.AlarmClock.addin.xml \
    AlarmMenu.xml

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

plugindir = $(PLUGINDIR)
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(RESOURCES_EXPANDED)
	$(MCS) $(MCS_FLAGS) -out:$(ASSEMBLY) -target:library $(BANSHEE_LIBS) $(RESOURCES_BUILD) $(SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = \
    $(SOURCES) \
    $(ASSEMBLY_GCONF_SCHEMA) \
    $(MENU_RESOURCE)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
