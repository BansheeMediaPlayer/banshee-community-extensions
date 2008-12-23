SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(srcdir)/AssemblyInfo.cs

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

plugindir = $(PLUGINDIR)
plugin_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE).mdb: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
	$(GMCS) $(MCS_FLAGS) -out:$@ -target:$(TARGET) $(REFERENCES) $(BANSHEE_LIBS) $(RESOURCES_BUILD) $(SOURCES_BUILD)

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in
