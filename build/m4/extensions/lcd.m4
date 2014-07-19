AC_DEFUN([BCE_LCD],
[
	BCE_ARG_DISABLE([LCD], [no])

	if test "x$enable_LCD" = "xyes"; then
		AM_CONDITIONAL(ENABLE_LCD, true)
	else
		AM_CONDITIONAL(ENABLE_LCD, false)
	fi
])

