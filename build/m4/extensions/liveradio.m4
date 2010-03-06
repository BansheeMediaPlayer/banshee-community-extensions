AC_DEFUN([BCE_LIVERADIO],
[
	BCE_ARG_DISABLE([LiveRadio], [yes])

	if test "x$enable_LiveRadio" = "xyes"; then
		AM_CONDITIONAL(ENABLE_LIVERADIO, true)
	else
		AM_CONDITIONAL(ENABLE_LIVERADIO, false)
	fi
])

