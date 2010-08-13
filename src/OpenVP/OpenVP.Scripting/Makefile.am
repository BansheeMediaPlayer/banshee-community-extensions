ASSEMBLY = OpenVP.Scripting
LINK =  -r:System -r:$(DIR_BIN)/Cdh.Affe.dll

SOURCES =  \
	AffeScript.cs \
	ScriptCompileException.cs \
	ScriptingEnvironment.cs \
	UserScript.cs

RESOURCES =  

if ENABLE_OPENVP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
