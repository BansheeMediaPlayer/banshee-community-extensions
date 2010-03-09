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

clean-local:
	rm -rf $(top_builddir)/bin

# Running Banshee with --uninstalled makes it create an addin db in the current directory.
# Mono.Addins looks for addins in a 'addins' folder in the addin db directory; so, we 
# copy the extension dlls to such a folder so Banshee will pick them up.
run:
	(cd bin; mkdir -p addins; cp *.dll* addins/; \
		export LD_LIBRARY_PATH=".${LD_LIBRARY_PATH+:$LD_LIBRARY_PATH}"; \
		banshee-1 --uninstalled --debug $(BANSHEE_DEV_OPTIONS) \
	)

if ENABLE_TESTS
# ---------------
# Unit Test Target
# ---------------

TEST_ASSEMBLIES = \
	Banshee.Mirage.dll

ENV_OPTIONS = TZ=America/Chicago LC_ALL=it_IT LANG=it_IT \
			  LD_LIBRARY_PATH="$(EXTENSION_DIR)/..:$(EXTENSION_DIR)${LD_LIBRARY_PATH+:$LD_LIBRARY_PATH}" \
			  MONO_PATH="$(EXTENSION_DIR)/..:$(EXTENSION_DIR)${MONO_PATH+:$MONO_PATH}"

RUNNER = for asm in $${TEST_ASSEMBLIES}; do echo -e "\033[1mRunning tests on $${asm}...\033[0m"; $(ENV_OPTIONS) nunit-console2 -nologo -noshadow $$asm; done

test:
	(cd bin; mkdir -p addins; cp *.dll* addins/; \
		export TEST_ASSEMBLIES="$(TEST_ASSEMBLIES)"; $(RUNNER); \
	)

endif

# ---------------
# Release Targets
# ---------------

-include release-rc

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

