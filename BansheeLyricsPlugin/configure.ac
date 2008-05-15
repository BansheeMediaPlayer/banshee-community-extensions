AC_INIT
AC_CONFIG_SRCDIR([README])
AC_CANONICAL_TARGET

AM_INIT_AUTOMAKE(BansheeLyricsPlugin, 1.0)
AM_MAINTAINER_MODE

#AC_PROG_INTLTOOL([0.23])

slicelocaledir='${prefix}/${DATADIRNAME}/locale'
AC_SUBST(slicelocaledir)

GNOME_COMPILE_WARNINGS
AM_CONFIG_HEADER(config.h)
AC_PATH_PROG(MONO, mono, no)
if test "x$MONO" = "xno"; then
	AC_MSG_ERROR([Cannot find "mono" runtime in your PATH])
fi

# HACK: made to use gmcs, since this is linking c# 2 for some reason
AC_PATH_PROG(MCS, gmcs, no)
if test "x$MCS" = "xno"; then
	AC_MSG_ERROR([Cannot find "mcs" compiler in your PATH])
fi

dnl pkg-config
#AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
#if test "x$PKG_CONFIG" = "xno"; then
#        AC_MSG_ERROR([You need to install pkg-config])
#fi



PKG_CHECK_MODULES([GTK_SHARP], [gtk-sharp-2.0 >= 2.0])
PKG_CHECK_MODULES([BANSHEE_1_SERVICES], [banshee-1-services])
PKG_CHECK_MODULES([BANSHEE_1_CORE], [banshee-1-core])
PKG_CHECK_MODULES([BANSHEE_1_THICKCLIENT], [banshee-1-thickclient])
PKG_CHECK_MODULES([GCONF_SHARP], [gconf-sharp-2.0 >= 2.0])
PKG_CHECK_MODULES([GTKHTML_SHARP_314], [gtkhtml-sharp-3.14])
AC_SUBST(GTK_SHARP_LIBS)
AC_SUBST(BANSHEE_1_SERVICES_LIBS)
AC_SUBST(BANSHEE_1_CORE_FLAGS)
AC_SUBST(BANSHEE_1_CORE_LIBS)
AC_SUBST(BANSHEE_1_THICKCLIENT_LIBS)
AC_SUBST(GCONF_SHARP_LIBS)
AC_SUBST(GTKHTML_SHARP_LIBS)

AC_ARG_ENABLE(user-plugin, AS_HELP_STRING([--enable-user-plugin],[Install plugin into user-level Banshee plugins directory instead of the Banshee system plugins directory]), , enable_user_plugin="no")

PLUGINDIR=`$PKG_CONFIG --variable=bansheedir banshee-1-core`
if test "x$enable_user_plugin" = "xyes"; then
	PLUGINDIR=`$PKG_CONFIG --variable=bansheedir banshee-1-core`
	PLUGINDIR=`echo "$PLUGINDIR" | sed -e "s|\~|$HOME|"`
fi
PLUGINDIR=`echo "$PLUGINDIR/Extensions"`
AC_SUBST(PLUGINDIR)

AC_CONFIG_FILES([
	Makefile
	src/Makefile
])
AC_OUTPUT

echo "
Plugin will be installed in $PLUGINDIR
You should now run \`make'
";

