ASSEMBLY = Mirage
LINK =  \
	-r:System \
	-r:System.Core
GMCS_FLAGS = -unsafe

SOURCES =  \
	Analyzer.cs \
	AudioDecoder.cs \
	Dbg.cs \
	Matrix.cs \
	Mfcc.cs \
	Scms.cs \
	ScmsConfiguration.cs \
	Vector.cs

RESOURCES =  \
	Resources/dct.filter \
	Resources/filterweights.filter

if ENABLE_MIRAGE
include $(top_srcdir)/build/build.mk
module_DATA = Mirage.dll.config
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
