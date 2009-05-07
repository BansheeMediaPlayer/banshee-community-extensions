
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = ../build/Banshee.OpenVP.Visualizations.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build

BANSHEE_OPENVP_VISUALIZATIONS_DLL_MDB_SOURCE=../build/Banshee.OpenVP.Visualizations.dll.mdb
BANSHEE_OPENVP_VISUALIZATIONS_DLL_MDB=$(BUILD_DIR)/Banshee.OpenVP.Visualizations.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = ../build/Banshee.OpenVP.Visualizations.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ../build

BANSHEE_OPENVP_VISUALIZATIONS_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(BANSHEE_OPENVP_VISUALIZATIONS_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(BANSHEE_OPENVP_VISUALIZATIONS_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	AssemblyInfo.cs \
	Core.cs \
	GlassWall.cs \
	GreenFlower.cs \
	Grid.cs \
	Inferno.cs \
	SimpleScope.cs \
	SpectrumAnalyzer.cs \
	Voiceprint.cs 

DATA_FILES = 

RESOURCES = resources/Banshee.OpenVP.Visualizations.addin.xml,resources.Banshee.OpenVP.Visualizations.addin.xml 

EXTRAS = \
	banshee.openvp.visualizations.pc.in 

REFERENCES =  \
	-pkg:openvp \
	-pkg:openvp.core \
	-pkg:tao-opengl \
	-pkg:tao-opengl-2.1 \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

BANSHEE_OPENVP_VISUALIZATIONS_PC = $(BUILD_DIR)/banshee.openvp.visualizations.pc

$(eval $(call emit-deploy-wrapper,BANSHEE_OPENVP_VISUALIZATIONS_PC,banshee.openvp.visualizations.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
