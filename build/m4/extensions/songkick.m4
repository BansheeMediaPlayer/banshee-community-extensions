AC_DEFUN([BCE_SONGKICK],
[
	BCE_ARG_DISABLE([Songkick], [yes])

	if test "x$enable_Songkick" = "xyes"; then
		AM_CONDITIONAL(ENABLE_SONGKICK, true)
	else
		AM_CONDITIONAL(ENABLE_SONGKICK, false)
	fi
])

