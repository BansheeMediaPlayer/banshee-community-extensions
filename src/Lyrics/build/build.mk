SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(srcdir)/AssemblyInfo.cs

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

ASSEMBLY_FILE = $(ASSEMBLY).dll

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

plugindir = $(PLUGINDIR)
plugin_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
	$(MCS) $(MCS_FLAGS) -out:$@ -target:library $(BANSHEE_LIBS) $(RESOURCES_BUILD) $(SOURCES_BUILD)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(ASSEMBLY_GCONF_SCHEMA)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
