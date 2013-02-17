AC_DEFUN([BCE_ANDROIDREMOTE],
[
	BCE_ARG_DISABLE([AndroidRemote], [yes])

	if test "x$enable_AndroidRemote" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ANDROIDREMOTE, true)
	else
		AM_CONDITIONAL(ENABLE_ANDROIDREMOTE, false)
	fi
])

