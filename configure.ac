AC_INIT([banshee-alarm-extension], [0.4.4])
AC_CONFIG_SRCDIR([README])

AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/shamrock \${ACLOCAL_FLAGS}"])

AC_PREREQ(2.13)
AM_INIT_AUTOMAKE

AM_MAINTAINER_MODE

IT_PROG_INTLTOOL([0.35.0])

dnl Mono programs
SHAMROCK_CHECK_MONO_MODULE(1.2.5)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME

MCS_FLAGS="-debug"
AC_SUBST(MCS_FLAGS)

dnl Banshee deps
PKG_CHECK_MODULES(BANSHEE, banshee-1-thickclient >= 1.0)
AC_SUBST(BANSHEE_LIBS)

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
fi

AC_SUBST(expanded_libdir)
AC_SUBST(PLUGINDIR)

dnl GNOME (optionnal)
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

dnl Internationalization support
SHAMROCK_CONFIGURE_I18N(banshee-alarm-plugin)

AC_CONFIG_FILES([
	Makefile
	po/Makefile.in
	build/Makefile
	src/Makefile
	src/AssemblyInfo.cs
	src/Banshee.AlarmClock.addin.xml
])
AC_OUTPUT

echo "
Extension will be installed in ${expanded_pluginsdir}
";
