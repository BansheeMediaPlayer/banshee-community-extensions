AC_PREREQ(2.52)
AC_INIT([banshee-community-extensions], 0.1,
	[http://bugzilla.gnome.org/enter_bug.cgi?product=banshee&component=Community%20Extensions])

AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar dist-zip foreign])
AM_MAINTAINER_MODE
AM_CONFIG_HEADER([config.h])
AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

IT_PROG_INTLTOOL([0.35.0])
AC_PROG_LIBTOOL
AC_PROG_INSTALL

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(1.9.1)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME

dnl Banshee deps
PKG_CHECK_MODULES(BANSHEE, banshee-1-thickclient >= 1.0)
AC_SUBST(BANSHEE_LIBS)

dnl Needed by Lyrics
PKG_CHECK_MODULES(GCONF_SHARP_20, gconf-sharp-2.0)
PKG_CHECK_MODULES(WEBKIT_SHARP, webkit-sharp-1.0 >= 0.2)

dnl Banshee Extensions Path
BANSHEE_LIBDIR=`$PKG_CONFIG banshee-1-core --variable=bansheedir`
SYSTEM_EXTENSIONS=$BANSHEE_LIBDIR/Extensions
REL_EXTENSIONS_DIR=`basename $SYSTEM_EXTENSIONS`
REL_BANSHEE_DIR=`echo "$SYSTEM_EXTENSIONS" | sed -e "s|\/$REL_EXTENSIONS_DIR||"`
REL_BANSHEE_DIR=`basename $REL_BANSHEE_DIR`
REL_EXTENSIONS_DIR=$REL_BANSHEE_DIR/$REL_EXTENSIONS_DIR

EXTENSION_DIR=$libdir/$REL_EXTENSIONS_DIR
AC_SUBST(EXTENSION_DIR)

expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
		   case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
		   eval echo $libdir )`
expanded_extensionsdir=$expanded_libdir/$REL_EXTENSIONS_DIR

dnl GNOME (optional)
AC_ARG_ENABLE(gnome, 
	AC_HELP_STRING([--disable-gnome], 
		[Disable GNOME support]), 
	, enable_gnome="yes")
if test "x$enable_gnome" = "xyes"; then
	AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
	AM_GCONF_SOURCE_2
	AM_CONDITIONAL(ENABLE_GNOME, true)
else
	AM_CONDITIONAL(GCONF_SCHEMAS_INSTALL, false)
	AM_CONDITIONAL(ENABLE_GNOME, false)
fi

dnl i18n
SHAMROCK_CONFIGURE_I18N($PACKAGE)

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use release configuration]),
	enable_release=yes, enable_release=`(test ! -d .svn && test ! -d .git) && echo "yes" || echo "no"`)
AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")
ENABLE_RELEASE=$enable_release
AC_SUBST(ENABLE_RELEASE)
if test "x$enable_release" = "xyes"; then
	GMCS_FLAGS="-define:RELEASE"
else
	GMCS_FLAGS="-warnaserror"
fi
AC_SUBST(GMCS_FLAGS)

dnl Mirage
PKG_CHECK_MODULES(FFTW3F, fftw3f)
PKG_CHECK_MODULES(SQLITE3, sqlite3 >= 3.3)
PKG_CHECK_MODULES(LIBSAMPLERATE, samplerate)
GSTREAMER_REQUIRED_VERSION=0.10.15
AC_SUBST(GSTREAMER_REQUIRED_VERSION)
PKG_CHECK_MODULES(GST,
    gstreamer-0.10 >= $GSTREAMER_REQUIRED_VERSION
    gstreamer-base-0.10 >= $GSTREAMER_REQUIRED_VERSION
    gstreamer-plugins-base-0.10 >= $GSTREAMER_REQUIRED_VERSION)
AC_SUBST(GST_CFLAGS)
AC_SUBST(GST_LIBS)
PKG_CHECK_MODULES(GLIB, glib-2.0)

AC_ARG_WITH(vendor-build-id,
	AC_HELP_STRING([--with-vendor-build-id=<vendor-build-id>],
		[Set a vendor build ID, recommended for packagers]),
	[vendor_build_id="$withval"], [
		test -d .git \
			&& vendor_build_id="git-checkout" \
			|| vendor_build_id="source-tarball"
	])
BUILD_VENDOR_ID="$vendor_build_id"
BUILD_HOST_OS="$host_os"
BUILD_HOST_CPU="$host_cpu"
BUILD_TIME=`date +"%F %T %Z"`
AC_SUBST(BUILD_VENDOR_ID)
AC_SUBST(BUILD_HOST_OS)
AC_SUBST(BUILD_HOST_CPU)
AC_SUBST(BUILD_TIME)

SHAVE_INIT([build/m4/shave], [enable])

dnl generated files
AC_OUTPUT([
Makefile

build/Makefile
build/m4/Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool

po/Makefile.in

src/Makefile
src/AlarmClock/Makefile
src/Mirage/Makefile
src/Mirage/libmirageaudio/Makefile
src/Mirage/Mirage/Makefile
src/Mirage/Banshee.Mirage/Makefile
src/Mirage/Test/Makefile
src/RadioStationFetcher/Makefile
src/RadioStationFetcher/RadioStationFetcher.addin.xml
])

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:    ${prefix}
    Datadir:           ${expanded_datadir}
    Libdir:            ${expanded_libdir}
    Extensions libdir: ${expanded_extensionsdir}

    C Compiler:        ${CC}
    Mono C# Compiler:  ${MCS} ${GMCS_FLAGS}
    Mono Runtime:      ${MONO}

  Build/Development:
    Release Build:     ${enable_release}
    Vendor Build ID:   ${vendor_build_id}

  Extensions:
    Alarm Clock:         yes
    Mirage:              yes
    RadioStationFetcher: yes

EOF
