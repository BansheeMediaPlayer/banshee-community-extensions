ASSEMBLY = Test
TARGET = exe

SOURCES = Main.cs

REFERENCES =  \
	-r:../Mirage/Mirage.dll

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(srcdir)/AssemblyInfo.cs

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(ASSEMBLY).$(ASSEMBLY_EXTENSION)

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

all: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED)
	$(MCS) $(MCS_FLAGS) -out:$@ -target:$(TARGET) $(REFERENCES) $(BANSHEE_LIBS) $(SOURCES_BUILD)

EXTRA_DIST = $(SOURCES_BUILD)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in
