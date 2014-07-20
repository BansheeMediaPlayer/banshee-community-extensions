AC_DEFUN([BCE_AWN],
[
	BCE_ARG_DISABLE([Awn], [yes])

	if test "x$enable_Awn" = "xyes"; then
		AM_CONDITIONAL(ENABLE_AWN, true)
	else
		AM_CONDITIONAL(ENABLE_AWN, false)
	fi
])

