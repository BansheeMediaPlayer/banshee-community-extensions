ASSEMBLY = Alarm.dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-alarm.schemas.in

MENU_RESOURCE = AlarmMenu.xml

SOURCES = \
	$(srcdir)/AlarmPlugin.cs \
	$(srcdir)/VolumeFade.cs \
	$(srcdir)/Alarm.cs \
	$(srcdir)/AlarmConfigDialog.cs \
	$(srcdir)/GConfSchemas.cs \
	$(srcdir)/SleepTimerConfigDialog.cs \
	$(srcdir)/ConfigurationWidget.cs \
	$(srcdir)/AssemblyInfo.cs

plugindir = $(PLUGINDIR)
plugin_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(MENU_RESOURCE)
	$(MCS) $(MCS_FLAGS) -out:$(ASSEMBLY) -target:library $(BANSHEE_LIBS) -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) $(SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = \
    $(SOURCES) \
    $(ASSEMBLY_GCONF_SCHEMA) \
    $(MENU_RESOURCE)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
