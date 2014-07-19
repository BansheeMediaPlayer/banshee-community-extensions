AC_DEFUN([BCE_FANART],
[
	BCE_ARG_DISABLE([FanArt], [yes])

	if test "x$enable_Fanart" = "xyes"; then
		AM_CONDITIONAL(ENABLE_FANART, true)
	else
		AM_CONDITIONAL(ENABLE_FANART, false)
	fi
])

