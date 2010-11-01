AC_DEFUN([BCE_AMPACHE],
[
	BCE_ARG_DISABLE([Ampache], [yes])

	if test "x$enable_Ampache" = "xyes"; then
		AM_CONDITIONAL(ENABLE_AMPACHE, true)
	else
		AM_CONDITIONAL(ENABLE_AMPACHE, false)
	fi
])

