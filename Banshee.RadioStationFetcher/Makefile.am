
EXTRA_DIST =  

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Banshee.RadioStationFetcher.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

BANSHEE_RADIOSTATIONFETCHER_DLL_MDB_SOURCE=bin/Debug/Banshee.RadioStationFetcher.dll.mdb
BANSHEE_RADIOSTATIONFETCHER_DLL_MDB=$(BUILD_DIR)/Banshee.RadioStationFetcher.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Banshee.RadioStationFetcher.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

BANSHEE_RADIOSTATIONFETCHER_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(BANSHEE_RADIOSTATIONFETCHER_EXE_MDB)  

BINARIES = \
	$(BANSHEE_RADIOSTATIONFETCHER)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES =  \
	src/FetcherDialog.cs \
	src/IFreetextSearchable.cs \
	src/IGenreSearchable.cs \
	src/RadioStationFetcherService.cs \
	src/Shoutcast.cs \
	src/Xiph.cs 

DATA_FILES = 

RESOURCES =  \
	Banshee.RadioStationFetcher.addin.xml \
	Resources/RadioStationFetcherMenu.xml,Resources.RadioStationFetcherMenu.xml 

REFERENCES =  \
	-pkg:banshee-1-core \
	-pkg:banshee-1-hyena \
	-pkg:banshee-1-services \
	-pkg:banshee-1-thickclient \
	-pkg:gtk-sharp-2.0 \
	System.Xml 

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(BINARIES) 

include $(top_srcdir)/Makefile.include

BANSHEE_RADIOSTATIONFETCHER = $(BUILD_DIR)/banshee.radiostationfetcher

$(eval $(call emit-deploy-wrapper,BANSHEE_RADIOSTATIONFETCHER,banshee.radiostationfetcher,x))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
