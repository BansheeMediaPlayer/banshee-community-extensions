ASSEMBLY = Test
TARGET = exe

SOURCES = Main.cs

REFERENCES =  \
	-r:../Mirage/Mirage.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

test: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
	$(GMCS) $(MCS_FLAGS) -out:$@ -target:$(TARGET) $(REFERENCES) $(BANSHEE_LIBS) $(SOURCES_BUILD)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in
