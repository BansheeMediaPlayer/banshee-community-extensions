AC_DEFUN([BCE_LYRICS],
[
	BCE_ARG_DISABLE([Lyrics], [try])

	BCE_CHECK_EXTENSION_DEP([Lyrics], [GCONF_SHARP_20],
		[gconf-sharp-2.0],
		[gconf-sharp was not found. Please install gconf-sharp or disable the Lyrics extension by passing --disable-lyrics])

	BCE_CHECK_EXTENSION_DEP([Lyrics], [BANSHEE_WEBBROWSER],
		[banshee-webbrowser],
		[banshee-webbrowser was not found. Please install it or disable the Lyrics extension by passing --disable-lyrics])

	if test "x$enable_Lyrics" = "xtry" \
		&& test "x$have_GCONF_SHARP_20" = "xyes" \
		&& test "x$have_BANSHEE_WEBBROWSER" = "xyes"; then
		enable_Lyrics=yes
	fi

	if test "x$enable_Lyrics" = "xyes"; then
		AM_CONDITIONAL(ENABLE_LYRICS, true)
	else
		enable_Lyrics=no
		AM_CONDITIONAL(ENABLE_LYRICS, false)
	fi
])

