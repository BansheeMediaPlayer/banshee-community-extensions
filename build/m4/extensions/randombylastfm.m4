AC_DEFUN([BCE_RANDOMBYLASTFM],
[
	BCE_ARG_DISABLE([RandomByLastfm], [yes])

	BCE_CHECK_EXTENSION_DEP([RandomByLastfm], [BANSHEE_LASTFM],
		[banshee-lastfm],
		[banshee-lastfm was not found. Please install it or disable the RandomByLastfm extension by passing --disable-randombylastfm])

	if test "x$enable_RandomByLastfm" = "xtry" \
		&& test "x$have_BANSHEE_LASTFM" = "xyes"; then
		enable_RandomByLastfm=yes
	fi

	if test "x$enable_RandomByLastfm" = "xyes"; then
		AM_CONDITIONAL(ENABLE_RANDOMBYLASTFM, true)
	else
		AM_CONDITIONAL(ENABLE_RANDOMBYLASTFM, false)
	fi
])

