ASSEMBLY = Cdh.Affe
LINK = -r:System

SOURCES =  \
	AffeCompiler.cs \
	affe-parser.cs \
	Lexer.cs \
	ScriptState.cs \
	Symbol.cs \
	SymbolTable.cs \
	Tree.cs

RESOURCES =  

JAY_FILES = jay/affe-parser.jay \
	jay/skeleton.cs

affe-parser.cs : $(JAY_FILES)
	$(JAY) -b .cs -c $< < jay/skeleton.cs > $@

if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
EXTRA_DIST += $(JAY_FILES)
else
EXTRA_DIST = $(SOURCES) $(RESOURCES) $(JAY_FILES)
endif
