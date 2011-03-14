AC_DEFUN([BCE_OPENVP],
[
	BCE_ARG_DISABLE([OpenVP], [try])

	BCE_CHECK_EXTENSION_DEP([ClutterFlow], [BANSHEE_NOWPLAYING],
		[banshee-nowplaying],
		[The Banshee NowPlaying extension was not found. Please install it or disable the OpenVP extension by passing --disable-openvp])

	BCE_CHECK_EXTENSION_DEP([OpenVP], [TAO_OPENGL],
		[tao-opengl >= 2.1],
		[The Tao.OpenGl library was not found. Please install it or disable the OpenVP extension by passing --disable-openvp])

	BCE_CHECK_EXTENSION_DEP([OpenVP], [TAO_FREEGLUT],
		[tao-freeglut >= 2.4],
		[The Tao.FreeGlut library was not found. Please install it or disable the OpenVP extension by passing --disable-openvp])

	BCE_CHECK_EXTENSION_DEP([OpenVP], [TAO_SDL],
		[tao-sdl >= 1.2.13],
		[The Tao.Sdl library was not found. Please install it or disable the OpenVP extension by passing --disable-openvp])

	AC_PATH_PROG(JAY, jay, no)
	if test "x$enable_OpenVP" = "xyes" -a "x$JAY" = "xno"; then
		AC_MSG_ERROR([The jay parser generator was not found. Please install it or disable the OpenVP extension by passing --disable-openvp])
	else
		if test "x$JAY" = "xno"; then
			have_JAY=no
		else
			have_JAY=yes
		fi
	fi

	if test "x$enable_OpenVP" = "xtry" \
		&& test "x$have_TAO_OPENGL" = "xyes" \
		&& test "x$have_TAO_FREEGLUT" = "xyes" \
		&& test "x$have_TAO_SDL" = "xyes" \
		&& test "x$have_JAY" = "xyes"; then
		enable_OpenVP=yes
	fi

	if test "x$enable_OpenVP" = "xyes"; then
		AM_CONDITIONAL(ENABLE_OPENVP, true)
	else
		enable_OpenVP=no
		AM_CONDITIONAL(ENABLE_OPENVP, false)
	fi
])
