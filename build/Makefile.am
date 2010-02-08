SUBDIRS = m4

GEN_GUID_ASSEMBLY = gen-guid.exe

ALL_TARGETS = $(GEN_GUID_ASSEMBLY)

all: $(ALL_TARGETS)

$(GEN_GUID_ASSEMBLY): gen-guid.cs
	$(MCS) -out:$@ $<

EXTRA_DIST = \
	gconf-schema-rules \
	gen-guid.cs \
	private-icon-theme-installer

CLEANFILES = *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in

