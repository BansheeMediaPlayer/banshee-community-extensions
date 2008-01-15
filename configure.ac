dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([Mirage], [0.2.1])
AM_INIT_AUTOMAKE([foreign])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

AM_PROG_LIBTOOL
AC_PROG_LIBTOOL

IT_PROG_INTLTOOL([0.35.0])
GETTEXT_PACKAGE=Mirage
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE", [Gettext Package])
AM_GLIB_GNU_GETTEXT

AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi
MCS=gmcs
AC_SUBST(MCS)

PKG_CHECK_MODULES(GTK_SHARP_20, gtk-sharp-2.0 >= 2.10)
PKG_CHECK_MODULES(GCONF_SHARP_20, gconf-sharp-2.0)
PKG_CHECK_MODULES(BANSHEE, banshee)
BANSHEE_SYSTEM_PLUGINS=`$PKG_CONFIG --variable=systemplugindir banshee`
AC_SUBST(BANSHEE_SYSTEM_PLUGINS)
BANSHEE_LIBDIR=`$PKG_CONFIG --variable=libdir banshee`"/banshee"
AC_SUBST(BANSHEE_LIBDIR)

dnl libmirageaudio
PKG_CHECK_MODULES(FFTW3F, fftw3f)
PKG_CHECK_MODULES(SQLITE3, sqlite3 >= 3.3)
PKG_CHECK_MODULES(LIBSAMPLERATE, samplerate)
GSTREAMER_REQUIRED_VERSION=0.10.3
AC_SUBST(GSTREAMER_REQUIRED_VERSION)
PKG_CHECK_MODULES(GST,
    gstreamer-0.10 >= $GSTREAMER_REQUIRED_VERSION
    gstreamer-base-0.10 >= $GSTREAMER_REQUIRED_VERSION
    gstreamer-plugins-base-0.10 >= $GSTREAMER_REQUIRED_VERSION)
AC_SUBST(GST_CFLAGS)
AC_SUBST(GST_LIBS)
PKG_CHECK_MODULES(GLIB, glib-2.0)

AC_ISC_POSIX
AC_PROG_CC
AC_HEADER_STDC
AM_PATH_GLIB_2_0


AC_CONFIG_FILES([
                 libmirageaudio/Makefile

                 Mirage/Makefile
                 Mirage/AssemblyInfo.cs
                 Mirage/res/Makefile

                 Banshee/Makefile
                 Banshee/AssemblyInfo.cs
                 Banshee/Config.cs
                 Banshee/res/Makefile

                 Test/Makefile

                 po/Makefile.in

                 Makefile
])

AC_OUTPUT
