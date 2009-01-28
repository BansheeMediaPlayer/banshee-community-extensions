
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+ -debug -define:DEBUG
ASSEMBLY = ../build/OpenVP.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build

OPENVP_DLL_MDB_SOURCE=../build/OpenVP.dll.mdb
OPENVP_DLL_MDB=$(BUILD_DIR)/OpenVP.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize+
ASSEMBLY = ../build/OpenVP.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build

OPENVP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(OPENVP_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(OPENVP_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	AssemblyInfo.cs \
	Color.cs \
	Effect.cs \
	IBeatDetector.cs \
	IController.cs \
	IPreset.cs \
	IRenderer.cs \
	KeyboardEvent.cs \
	LinearPreset.cs \
	Metadata/AuthorAttribute.cs \
	Metadata/BrowsableAttribute.cs \
	Metadata/CategoryAttribute.cs \
	Metadata/DefaultValueAttribute.cs \
	Metadata/DescriptionAttribute.cs \
	Metadata/DisplayNameAttribute.cs \
	Metadata/FollowsAttribute.cs \
	Metadata/RangeAttribute.cs \
	NullPlayerData.cs \
	PlayerData.cs \
	Registry.cs \
	SDLController.cs \
	TimedPresetBase.cs \
	UDPPlayerData.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	openvp.pc.in 

REFERENCES =  \
	-pkg:tao-freeglut \
	-pkg:tao-opengl \
	-pkg:tao-sdl \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

OPENVP_PC = $(BUILD_DIR)/openvp.pc

$(eval $(call emit-deploy-wrapper,OPENVP_PC,openvp.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
