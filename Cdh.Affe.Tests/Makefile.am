
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = ../build/Cdh.Affe.Tests.exe
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../build/Cdh.Affe.dll
BUILD_DIR = ../build

CDH_AFFE_DLL_SOURCE=../build/Cdh.Affe.dll
CDH_AFFE_TESTS_EXE_MDB_SOURCE=../build/Cdh.Affe.Tests.exe.mdb
CDH_AFFE_TESTS_EXE_MDB=$(BUILD_DIR)/Cdh.Affe.Tests.exe.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = ../build/Cdh.Affe.Tests.exe
ASSEMBLY_MDB = 
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../build/Cdh.Affe.dll
BUILD_DIR = ../build

CDH_AFFE_DLL_SOURCE=../build/Cdh.Affe.dll
CDH_AFFE_TESTS_EXE_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(CDH_AFFE_DLL) \
	$(CDH_AFFE_TESTS_EXE_MDB)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) 

FILES =  \
	AssemblyInfo.cs \
	Main.cs \
	Tests.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	../build/Cdh.Affe.dll \
	-pkg:nunit \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) 

include $(top_srcdir)/Makefile.include

CDH_AFFE_DLL = $(BUILD_DIR)/Cdh.Affe.dll



$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
