ASSEMBLY = Mirage
LINK =  \
	-r:System \
	-r:System.Data \
	-r:Mono.Data.SqliteClient
GMCS_FLAGS = -unsafe

SOURCES =  \
	AudioDecoder.cs \
	Db.cs \
	Dbg.cs \
	Matrix.cs \
	Mfcc.cs \
	Analyzer.cs \
	MirageDbMigrator.cs \
	Scms.cs \
	Vector.cs

RESOURCES =  \
	Resources/dct.filter \
	Resources/filterweights.filter

include $(top_srcdir)/build/build.mk
