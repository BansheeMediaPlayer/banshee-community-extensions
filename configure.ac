dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([Banshee.RadioStationFetcher], [0.1])
AM_INIT_AUTOMAKE([foreign])
AM_MAINTAINER_MODE

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

IT_PROG_INTLTOOL([0.35.0])
GETTEXT_PACKAGE=$PACKAGE
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE", [Gettext Package])
AM_GLIB_GNU_GETTEXT

AC_PROG_INSTALL

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'DEBUG' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'RELEASE' Configuration [default=YES]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	AM_CONDITIONAL(ENABLE_RELEASE, true)
	enable_release=yes
fi


dnl package checks, common for all configs
PKG_CHECK_MODULES(GTKSHARP, gtk-sharp-2.0 >= 2.12)

PKG_CHECK_MODULES(BANSHEE, banshee-1-hyena >= 1.4.0 
                           banshee-1-core >= 1.4.0
                           banshee-1-services >= 1.4.0
                           banshee-1-thickclient >= 1.4.0)

AC_ARG_ENABLE(user-extension, AC_HELP_STRING([--enable-user-extension], [Install extension into user-level Banshee extensions directory instead of the Banshee system extensions directory]), , enable_user_extension="no")

dnl Banshee Extensions Path
SYSTEM_PLUGINS=`$PKG_CONFIG --variable=bansheedir banshee-1-core`/Extensions
REL_PLUGINS_DIR=`basename $SYSTEM_PLUGINS`
REL_BANSHEE_DIR=`echo "$SYSTEM_PLUGINS" | sed -e "s|\/$REL_PLUGINS_DIR||"`
REL_BANSHEE_DIR=`basename $REL_BANSHEE_DIR`
REL_PLUGINS_DIR=$REL_BANSHEE_DIR/$REL_PLUGINS_DIR

EXTENSION_DIR=$libdir/$REL_PLUGINS_DIR
expanded_pluginsdir=$expanded_libdir/$REL_PLUGINS_DIR

if test "x$enable_user_extension" = "xyes"; then
 	EXTENSION_DIR=${XDG_CONFIG_HOME:-~/.config}/banshee-1/addins
	EXTENSION_DIR=`echo "$EXTENSION_DIR" | sed -e "s|\~|$HOME|"`
	expanded_pluginsdir=$EXTENSION_DIR
fi
 
AC_SUBST(EXTENSION_DIR)

dnl package checks, per config


dnl Path to the banshee installation, to find the the gconf-schema-extractor.exe tool
banshee_libdir=`$PKG_CONFIG --variable=bansheedir banshee-1-core`
AC_SUBST(banshee_libdir)


AC_ARG_ENABLE(user-extension, AC_HELP_STRING([--enable-user-extension], [Install extension into user-level Banshee extensions directory instead of the Banshee system extensions directory]), , enable_user_extension="no")

dnl Banshee Extensions Path
SYSTEM_PLUGINS=`$PKG_CONFIG --variable=bansheedir banshee-1-core`/Extensions
REL_PLUGINS_DIR=`basename $SYSTEM_PLUGINS`
REL_BANSHEE_DIR=`echo "$SYSTEM_PLUGINS" | sed -e "s|\/$REL_PLUGINS_DIR||"`
REL_BANSHEE_DIR=`basename $REL_BANSHEE_DIR`
REL_PLUGINS_DIR=$REL_BANSHEE_DIR/$REL_PLUGINS_DIR


expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
		   case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
		   eval echo $libdir )`
		   
PLUGINDIR=$libdir/$REL_PLUGINS_DIR
expanded_pluginsdir=$expanded_libdir/$REL_PLUGINS_DIR

if test "x$enable_user_extension" = "xyes"; then
 	PLUGINDIR=${XDG_CONFIG_HOME:-~/.config}/banshee-1/addins
	PLUGINDIR=`echo "$PLUGINDIR" | sed -e "s|\~|$HOME|"`
	expanded_pluginsdir=$PLUGINDIR
	echo "USER EXTENSION ENABLED!";
fi


AC_SUBST(PLUGINDIR)
AC_SUBST(expanded_pluginsdir)



AC_CONFIG_FILES([
Banshee.RadioStationFetcher/Makefile
po/Makefile.in
Makefile
])

AC_OUTPUT
