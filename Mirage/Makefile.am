SUBDIRS = res

MCS_FLAGS= -noconfig -debug -codepage:utf8 -unsafe -warn:4 -d:DEBUG

ASSEMBLY = Mirage.dll

SOURCES = \
	AudioDecoder.cs \
	CovarianceMatrix.cs \
	Db.cs \
	Dbg.cs \
	Matrix.cs \
	Mfcc.cs \
	Mir.cs \
	Scms.cs \
	Vector.cs \
	AssemblyInfo.cs

RESOURCES = \
	-resource:res/dct.filter \
	-resource:res/filterweights.filter 

REFERENCES =  \
	-r:Mono.Posix \
	-r:Mono.Data \
	-r:Mono.Data.SqliteClient \
	-r:System.Data \
	-r:System

all: $(ASSEMBLY)

$(ASSEMBLY) $(ASSEMBLY).mdb: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$(ASSEMBLY) $(RESOURCES) $(REFERENCES) -target:library $(BANSHEE_LIBS) $(SOURCES)

EXTRA_DIST = \
	$(SOURCES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

miragedir = $(BANSHEE_LIBDIR)
mirage_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb
