AC_INIT(README)

AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)

VERSION=0.2
AM_INIT_AUTOMAKE(banshee-alarm-plugin, $VERSION)

AM_MAINTAINER_MODE

AC_PROG_INTLTOOL([0.21])

MONO_REQUIRED_VERSION=1.1.13
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)

dnl Mono programs
MONODIR=`$PKG_CONFIG --variable=exec_prefix mono`
MONOBIN="$MONODIR/bin"
MONO="$MONOBIN/mono"
MCS="$MONOBIN/gmcs"

if test ! -x "$MONO"; then
	AC_MSG_ERROR([No Mono runtime found])
fi

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS 2.0 (gmcs) compiler found])
fi

MCS_FLAGS="-debug"

dnl Banshee deps
PKG_CHECK_MODULES(BANSHEE, banshee >= 0.11.4)
AC_SUBST(BANSHEE_LIBS)

AC_SUBST(MCS_FLAGS)
AC_SUBST(MCS)
AC_SUBST(MONO)

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

dnl GConf
AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
AM_GCONF_SOURCE_2

dnl Internationalization support
dnl ALL_LINGUAS="`cat "$srcdir/po/LINGUAS" | grep -v '^#'`"
AM_GLIB_GNU_GETTEXT

AC_OUTPUT([
	Makefile
	po/Makefile.in
	build/Makefile
	src/Makefile
	src/AssemblyInfo.cs
])

echo "
Plugin will be installed in ${expanded_pluginsdir}
";

