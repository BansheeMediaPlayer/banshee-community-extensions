
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = ../build/OpenVP.Core.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../build/OpenVP.dll \
	../build/OpenVP.Scripting.dll \
	../build/Cdh.Affe.dll
BUILD_DIR = ../build

OPENVP_DLL_SOURCE=../build/OpenVP.dll
CDH_AFFE_DLL_SOURCE=../build/Cdh.Affe.dll
OPENVP_CORE_DLL_MDB_SOURCE=../build/OpenVP.Core.dll.mdb
OPENVP_CORE_DLL_MDB=$(BUILD_DIR)/OpenVP.Core.dll.mdb
OPENVP_SCRIPTING_DLL_SOURCE=../build/OpenVP.Scripting.dll

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = ../build/OpenVP.Core.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../build/OpenVP.dll \
	../build/OpenVP.Scripting.dll \
	../build/Cdh.Affe.dll
BUILD_DIR = ../build

OPENVP_DLL_SOURCE=../build/OpenVP.dll
CDH_AFFE_DLL_SOURCE=../build/Cdh.Affe.dll
OPENVP_CORE_DLL_MDB=
OPENVP_SCRIPTING_DLL_SOURCE=../build/OpenVP.Scripting.dll

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(OPENVP_DLL) \
	$(CDH_AFFE_DLL) \
	$(OPENVP_CORE_DLL_MDB) \
	$(OPENVP_SCRIPTING_DLL)  

LINUX_PKGCONFIG = \
	$(OPENVP_CORE_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	AssemblyInfo.cs \
	Buffer.cs \
	BurstScope.cs \
	ClearScreen.cs \
	DynamicMovement.cs \
	Laser.cs \
	Mirror.cs \
	MovementBase.cs \
	Scope.cs \
	ScopeBase.cs \
	SharedTextureHandle.cs \
	SuperScope.cs \
	TextureHandle.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	openvp.core.pc.in 

REFERENCES =  \
	../build/Cdh.Affe.dll \
	../build/OpenVP.dll \
	../build/OpenVP.Scripting.dll \
	Mono.Posix \
	-pkg:tao-freeglut \
	-pkg:tao-opengl \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

OPENVP_DLL = $(BUILD_DIR)/OpenVP.dll
CDH_AFFE_DLL = $(BUILD_DIR)/Cdh.Affe.dll
OPENVP_CORE_PC = $(BUILD_DIR)/openvp.core.pc
OPENVP_SCRIPTING_DLL = $(BUILD_DIR)/OpenVP.Scripting.dll

$(eval $(call emit-deploy-wrapper,OPENVP_CORE_PC,openvp.core.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
