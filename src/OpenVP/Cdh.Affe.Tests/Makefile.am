ASSEMBLY = Cdh.Affe.Tests
LINK =  -r:System -r:$(DIR_BIN)/Cdh.Affe.dll
TEST_ASSEMBLY = yes

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
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
