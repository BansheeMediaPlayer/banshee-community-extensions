AC_DEFUN([BCE_AMPACHE],
[
	BCE_ARG_DISABLE([Ampache], [yes])

	BCE_CHECK_EXTENSION_DEP([Ampache], [BANSHEE_190],
		[banshee-1-services >= 1.9.0],
		[Banshee 1.9.0 was not found. Please install it or disable the Ampache extension by passing --disable-ampache])

	if test "x$enable_Ampache" = "xyes" \
		&& test "x$have_BANSHEE_190" = "xyes"; then
		AM_CONDITIONAL(ENABLE_AMPACHE, true)
	else
		AM_CONDITIONAL(ENABLE_AMPACHE, false)
	fi
])

