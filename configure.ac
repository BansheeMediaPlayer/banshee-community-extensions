dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([OpenVP], [0.1])
AM_INIT_AUTOMAKE([foreign])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

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

PKG_CHECK_MODULES([NUNIT], [nunit])

dnl First package naming style is used on SUSE, second is used on Ubuntu
PKG_CHECK_MODULES(TAOOPENGL, tao-opengl,, [PKG_CHECK_MODULES(TAOOPENGL, Tao.Opengl)])
AC_SUBST(TAOOPENGL_LIBS)

PKG_CHECK_MODULES(TAOFREEGLUT, tao-freeglut,, [PKG_CHECK_MODULES(TAOFREEGLUT, Tao.Freeglut)])
AC_SUBST(TAOFREEGLUT_LIBS)

PKG_CHECK_MODULES(TAOSDL, tao-sdl,, [PKG_CHECK_MODULES(TAOSDL, Tao.Sdl)])
AC_SUBST(TAOSDL_LIBS)

dnl package checks, per config


AC_CONFIG_FILES([
OpenVP.Scripting/openvp.scripting.pc
Cdh.Affe.Tests/Makefile
OpenVP.Core/Makefile
OpenVP.Core/openvp.core.pc
OpenVP/openvp.pc
OpenVP.Scripting/Makefile
OpenVP/Makefile
Makefile
Cdh.Affe/Makefile
Cdh.Affe/cdh.affe.pc

])

AC_OUTPUT
