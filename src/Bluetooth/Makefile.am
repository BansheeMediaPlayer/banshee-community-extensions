ASSEMBLY = Banshee.Dap.Bluetooth
EXTRA_DEPS = update_submodule

if ENABLE_BLUETOOTH
update_submodule:
	if test -d "$(top_builddir)/.git"; then \
		git submodule update --init --recursive gnome-bluetooth-sharp || exit 1; \
	fi

include $(top_srcdir)/build/xbuild.mk

else
endif
