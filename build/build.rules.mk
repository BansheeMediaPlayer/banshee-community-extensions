UNIQUE_FILTER_PIPE = tr [:space:] \\n | sort | uniq
BUILD_DATA_DIR = $(top_builddir)/bin/share/$(PACKAGE)

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(top_srcdir)/src/AssemblyInfo.cs

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

INSTALL_ICONS = $(top_srcdir)/build/private-icon-theme-installer "$(mkinstalldirs)" "$(INSTALL_DATA)"
THEME_ICONS_SOURCE = $(wildcard $(srcdir)/ThemeIcons/*/*/*.png) $(wildcard $(srcdir)/ThemeIcons/scalable/*/*.svg)
THEME_ICONS_RELATIVE = $(subst $(srcdir)/ThemeIcons/, , $(THEME_ICONS_SOURCE))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

if ENABLE_TESTS
LINK += " $(NUNIT_LIBS) $(MONO_NUNIT_LIBS)"
ENABLE_TESTS_FLAG = "-define:ENABLE_TESTS"

test: $(ASSEMBLY_FILE)
	if test "x$(TEST_ASSEMBLY)" = "xyes"; then \
		LD_LIBRARY_PATH="$(BANSHEE_LIBDIR):$(BANSHEE_EXTDIR):$(EXTENSION_DIR)/..:$(EXTENSION_DIR)${LD_LIBRARY_PATH+:$LD_LIBRARY_PATH}" \
		MONO_PATH="$(BANSHEE_LIBDIR):$(BANSHEE_EXTDIR):$(EXTENSION_DIR)/..:$(EXTENSION_DIR)${MONO_PATH+:$MONO_PATH}" \
		$(NUNIT_CONSOLE) -nologo -noshadow $(ASSEMBLY_FILE); \
	fi

else

test:

endif

check: test


FILTERED_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE))
DEP_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE) | sed s,-r:,,g | grep '$(top_builddir)/bin/')

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

moduledir = $(EXTENSION_DIR)
module_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE) theme-icons

run: 
	@pushd $(top_builddir); \
	make run \
	popd;

build-debug:
	@echo $(DEP_LINK)

$(ASSEMBLY_FILE).mdb: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	$(MCS) \
		$(GMCS_FLAGS) \
		$(ENABLE_TESTS_FLAG) \
		$(ASSEMBLY_BUILD_FLAGS) \
		-debug -target:$(TARGET) -out:$@ \
		$(BUILD_DEFINES) \
		$(FILTERED_LINK) $(RESOURCES_BUILD) $(SOURCES_BUILD)
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

theme-icons: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -il "$(BUILD_DATA_DIR)" "$(srcdir)" $(THEME_ICONS_RELATIVE)

install-data-hook: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -i "$(DESTDIR)$(pkgdatadir)" "$(srcdir)" $(THEME_ICONS_RELATIVE)
	$(EXTRA_INSTALL_DATA_HOOK)

uninstall-hook: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -u "$(DESTDIR)$(pkgdatadir)" "$(srcdir)" $(THEME_ICONS_RELATIVE)
	$(EXTRA_UNINSTALL_HOOK)

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(THEME_ICONS_SOURCE)

CLEANFILES = $(OUTPUT_FILES) TestResult.xml
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

