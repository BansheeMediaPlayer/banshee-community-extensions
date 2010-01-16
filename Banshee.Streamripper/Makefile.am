
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Banshee.Streamripper.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

BANSHEE_STREAMRIPPER_DLL_MDB_SOURCE=bin/Debug/Banshee.Streamripper.dll.mdb
BANSHEE_STREAMRIPPER_DLL_MDB=$(BUILD_DIR)/Banshee.Streamripper.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Banshee.Streamripper.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

BANSHEE_STREAMRIPPER_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(BANSHEE_STREAMRIPPER_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(BANSHEE_STREAMRIPPER_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	Banshee.Streamripper/RippedFileScanner.cs \
	Banshee.Streamripper/StreamripperConfigDialog.cs \
	Banshee.Streamripper/StreamripperProcess.cs \
	Banshee.Streamripper/StreamripperService.cs 

DATA_FILES = 

RESOURCES =  \
	Banshee.Streamripper.addin.xml \
	Resources/StreamripperMenu.xml,Resources.StreamripperMenu.xml 

REFERENCES =  \
	-pkg:banshee-1-core \
	-pkg:banshee-1-hyena \
	-pkg:banshee-1-services \
	-pkg:banshee-1-thickclient \
	-pkg:gtk-sharp-2.0 \
	System

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

BANSHEE_STREAMRIPPER_PC = $(BUILD_DIR)/banshee.streamripper.pc

$(eval $(call emit-deploy-wrapper,BANSHEE_STREAMRIPPER_PC,banshee.streamripper.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
