AC_DEFUN([BCE_COVERWALLPAPER],
[
	BCE_ARG_DISABLE([CoverWallpaper], [yes])

	BCE_CHECK_EXTENSION_DEP([CoverWallpaper], [GCONFSHARP],
		[gconf-sharp-2.0],
		[GConf-sharp was not found. Please install it or disable the CoverWallpaper extension by passing --disable-coverwallpaper])

	if test "x$enable_CoverWallpaper" = "xtry" \
		&& test "x$have_GCONFSHARP" = "xyes"; then
		enable_CoverWallpaper=yes
	fi

	if test "x$enable_CoverWallpaper" = "xyes"; then
		AM_CONDITIONAL(ENABLE_COVERWALLPAPER, true)
	else
		AM_CONDITIONAL(ENABLE_COVERWALLPAPER, false)
	fi
])

