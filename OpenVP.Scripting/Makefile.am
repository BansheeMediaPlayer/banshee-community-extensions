
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = ../build/OpenVP.Scripting.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../build/Cdh.Affe.dll
BUILD_DIR = ../build

CDH_AFFE_DLL_SOURCE=../build/Cdh.Affe.dll
OPENVP_SCRIPTING_DLL_MDB_SOURCE=../build/OpenVP.Scripting.dll.mdb
OPENVP_SCRIPTING_DLL_MDB=$(BUILD_DIR)/OpenVP.Scripting.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = ../build/OpenVP.Scripting.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../build/Cdh.Affe.dll
BUILD_DIR = ../build

CDH_AFFE_DLL_SOURCE=../build/Cdh.Affe.dll
OPENVP_SCRIPTING_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(CDH_AFFE_DLL) \
	$(OPENVP_SCRIPTING_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(OPENVP_SCRIPTING_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	AffeScript.cs \
	AssemblyInfo.cs \
	ScriptCompileException.cs \
	ScriptingEnvironment.cs \
	UserScript.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	openvp.scripting.pc.in 

REFERENCES =  \
	../build/Cdh.Affe.dll \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

CDH_AFFE_DLL = $(BUILD_DIR)/Cdh.Affe.dll
OPENVP_SCRIPTING_PC = $(BUILD_DIR)/openvp.scripting.pc

$(eval $(call emit-deploy-wrapper,OPENVP_SCRIPTING_PC,openvp.scripting.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
