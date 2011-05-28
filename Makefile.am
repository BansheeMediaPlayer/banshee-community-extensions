ACLOCAL_AMFLAGS = -I build/m4/shamrock -I build/m4/shave -I build/m4/extensions ${ACLOCAL_FLAGS}

SUBDIRS = \
	build \
	help \
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

clean-local:
	rm -rf $(top_builddir)/bin

# Running Banshee with --uninstalled makes it create an addin db in the current directory.
# Mono.Addins looks for addins in a 'addins' folder in the addin db directory; so, we 
# copy the extension dlls to such a folder so Banshee will pick them up.
run:
	(cd bin; mkdir -p addins; cp *.dll* addins/; \
		export LD_LIBRARY_PATH=".${LD_LIBRARY_PATH+:$LD_LIBRARY_PATH}"; \
		banshee --uninstalled --debug $(BANSHEE_DEV_OPTIONS) \
	)

test: check

all-local: update-pot

update-pot:
	if [ "x$(top_srcdir)" = "x$(top_builddir)" ]; then \
		$(MAKE) -C "$(top_builddir)/po" $(GETTEXT_PACKAGE).pot; \
	fi;

# ---------------
# Release Targets
# ---------------

RELEASE_WEB_USER=banshee
RELEASE_WEB_HOST=banshee.fm
RELEASE_WEB_PATH=/home/banshee/web/download.banshee.fm/banshee-community-extensions

prepare-release: distcheck
	rm -rf release-data
	mkdir release-data
	cp NEWS release-data/$(PACKAGE)-$(VERSION).news
	git log > release-data/$(PACKAGE)-$(VERSION).changes
	find -maxdepth 1 \( -name \*.zip -o -name \*.bz2 -o -name \*.gz \) -exec cp -a {} release-data \;
	cd release-data && sha256sum * > $(PACKAGE)-$(VERSION).sha256sum

upload-release:
	scp -r release-data $(RELEASE_WEB_USER)@$(RELEASE_WEB_HOST):$(RELEASE_WEB_PATH)/$(VERSION)
	ssh $(RELEASE_WEB_USER)@$(RELEASE_WEB_HOST) rm -f $(RELEASE_WEB_PATH)/LATEST-IS\*
	ssh $(RELEASE_WEB_USER)@$(RELEASE_WEB_HOST) ln -s $(VERSION) $(RELEASE_WEB_PATH)/LATEST-IS-$(VERSION)

tag-release:
	git tag -a -m "$(VERSION) release" $(VERSION)
	git push origin $(VERSION)

release: prepare-release upload-release tag-release
	rm -rf release-data

