MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY = Test.exe

SOURCES = \
		  Main.cs

REFERENCES =  \
	-r:../Mirage/Mirage.dll

all: $(ASSEMBLY)

$(ASSEMBLY) $(ASSEMBLY).mdb: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$(ASSEMBLY) $(RESOURCES) $(REFERENCES) -target:exe $(BANSHEE_LIBS) $(SOURCES)

EXTRA_DIST = \
	$(SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

extra_DIST = $(ASSEMBLY) $(ASSEMBLY).mdb
