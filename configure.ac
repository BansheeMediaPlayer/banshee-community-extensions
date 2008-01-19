AC_INIT([banshee-alarm-plugin], [0.3.1])
AC_CONFIG_SRCDIR([README])

AC_CANONICAL_TARGET
AC_PREREQ(2.13)
AM_INIT_AUTOMAKE

AM_MAINTAINER_MODE

IT_PROG_INTLTOOL([0.35.0])

MONO_REQUIRED_VERSION=1.1.13
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)

dnl Mono programs
SHAMROCK_CHECK_MONO_MODULE(1.1.13)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME

MCS_FLAGS="-debug"

dnl Banshee deps
PKG_CHECK_MODULES(BANSHEE, banshee >= 0.11.4)
AC_SUBST(BANSHEE_LIBS)

AC_SUBST(MCS_FLAGS)

AC_ARG_ENABLE(user-plugin, AC_HELP_STRING([--enable-user-plugin], [Install plugin into user-level Banshee plugins directory instead of the Banshee system plugins directory]), , enable_user_plugin="no")

dnl Banshee Plugins Path
SYSTEM_PLUGINS=`$PKG_CONFIG --variable=systemplugindir banshee`
REL_PLUGINS_DIR=`basename $SYSTEM_PLUGINS`
REL_BANSHEE_DIR=`echo "$SYSTEM_PLUGINS" | sed -e "s|\/$REL_PLUGINS_DIR||"`
REL_BANSHEE_DIR=`basename $REL_BANSHEE_DIR`
REL_PLUGINS_DIR=$REL_BANSHEE_DIR/$REL_PLUGINS_DIR

expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
		   case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
		   eval echo $libdir )`
		   
PLUGINDIR=$libdir/$REL_PLUGINS_DIR
expanded_pluginsdir=$expanded_libdir/$REL_PLUGINS_DIR

if test "x$enable_user_plugin" = "xyes"; then
	PLUGINDIR=`$PKG_CONFIG --variable=userplugindir banshee`
	PLUGINDIR=`echo "$PLUGINDIR" | sed -e "s|\~|$HOME|"`
	expanded_pluginsdir=$PLUGINDIR
fi

AC_SUBST(expanded_libdir)
AC_SUBST(PLUGINDIR)

dnl Path to the banshee installation, to find the the gconf-schema-extractor.exe tool
banshee_libdir=`$PKG_CONFIG --variable=libdir banshee`
AC_SUBST(banshee_libdir)

dnl GConf
AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
AM_GCONF_SOURCE_2

dnl Internationalization support
SHAMROCK_CONFIGURE_I18N(banshee-alarm-plugin)

AC_CONFIG_FILES([
	Makefile
	po/Makefile.in
	build/Makefile
	src/Makefile
	src/AssemblyInfo.cs
])
AC_OUTPUT

echo "
Plugin will be installed in ${expanded_pluginsdir}
";

