SUBDIRS = \
	build \
	src \
	po

EXTRA_DIST = \
	intltool-extract.in \
	intltool-merge.in \
	intltool-update.in \
	AUTHORS

CLEANFILES = \
	intltool-extract \
	intltool-update \
	intltool-merge

DISTCLEANFILES = \
	*.bak \
	*~ \
	*.pidb

MAINTAINERCLEANFILES = \
	compile \
	INSTALL \
	config.h.in \
	aclocal.m4 \
	ltmain.sh \
	Makefile.in \
	depcomp \
	missing \
	install-sh \
	configure \
	config.sub \
	config.guess \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	mkinstalldirs

# Running Banshee with --uninstalled makes it create an addin db in the current directory.
# Mono.Addins looks for addins in a 'addins' folder in the addin db directory; so, we 
# copy the extension dlls to such a folder so Banshee will pick them up.
run:
	(cp src/Mirage/libmirageaudio/.libs/libmirageaudio.so bin/; \
		cd bin; mkdir -p addins; cp *.dll* addins/; \
		banshee-1 --uninstalled --debug $(BANSHEE_DEV_OPTIONS) \
	)
