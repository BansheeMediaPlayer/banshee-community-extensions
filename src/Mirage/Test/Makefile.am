ASSEMBLY = Test
TARGET = exe

SOURCES = Main.cs

REFERENCES =  \
	$(BANSHEE_LIBS) \
	-r:$(top_builddir)/bin/Mirage.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

test: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
	$(MCS) $(MCS_FLAGS) -out:$@ -target:$(TARGET) $(REFERENCES) $(SOURCES_BUILD)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in
