ASSEMBLY = Cdh.Affe.Tests
TARGET = exe
LINK =  -r:System -r:$(DIR_BIN)/Cdh.Affe.dll

SOURCES =  \
	Main.cs \
	Tests.cs

RESOURCES =  

if ENABLE_TESTS
if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
endif
