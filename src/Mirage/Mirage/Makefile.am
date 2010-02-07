ASSEMBLY = Mirage
TARGET = library

SOURCES = \
	AudioDecoder.cs \
	Db.cs \
	Dbg.cs \
	Matrix.cs \
	Mfcc.cs \
	Mir.cs \
	MirageDbMigrator.cs \
	Scms.cs \
	Vector.cs

RESOURCES = \
	res/dct.filter \
	res/filterweights.filter 

REFERENCES =  \
	-r:System \
	-r:System.Data \
	-r:Mono.Data.SqliteClient

include $(top_srcdir)/build/build.mk
