include $(top_srcdir)/build/build.environment.mk

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(BIN_DIR)/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)
PROJECT_FILE = $(wildcard $(srcdir)/*.[cf]sproj)

all: $(ASSEMBLY_FILE).mdb

$(ASSEMBLY_FILE).mdb: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(EXTRA_DEPS)
	$(XBUILD) $(PROJECT_FILE) /target:Build

clean:
	$(XBUILD) $(PROJECT_FILE) /target:Clean

.PHONY: $(ASSEMBLY_FILE).mdb

