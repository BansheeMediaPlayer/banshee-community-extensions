ASSEMBLY = Banshee.Dap.Bluetooth
LINK = $(BANSHEE_LIBS)

if ENABLE_BLUETOOTH
include $(top_srcdir)/build/build.mk

update_submodule:
	if test -d "$(top_builddir)/.git"; then \
		git submodule update --init --recursive gnome-bluetooth-sharp || exit 1; \
	fi

$(ASSEMBLY_FILE): update_submodule
	@mkdir -p $(top_builddir)/bin
	xbuild Banshee.Dap.Bluetooth.fsproj
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

clean:
	xbuild Banshee.Dap.Bluetooth.fsproj /target:clean

.PHONY: $(ASSEMBLY_FILE)

else
endif
