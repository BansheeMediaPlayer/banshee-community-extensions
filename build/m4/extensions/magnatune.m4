AC_DEFUN([BCE_MAGNATUNE],
[
	BCE_ARG_DISABLE([Magnatune], [yes])

	if test "x$enable_Magnatune" = "xyes"; then
		AM_CONDITIONAL(ENABLE_MAGNATUNE, true)
	else
		AM_CONDITIONAL(ENABLE_MAGNATUNE, false)
	fi
])

