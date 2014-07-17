AC_DEFUN([BCE_BLUETOOTH],
[
	BCE_ARG_DISABLE([Bluetooth], [yes])

	if test "x$enable_Bluetooth" = "xyes"; then
		AM_CONDITIONAL(ENABLE_BLUETOOTH, true)

		AC_PATH_PROG(FSC, fsharpc, no)
		if test "x$FSC" = "xno"; then
			AC_MSG_ERROR([`You need to install an F♯ compiler.'])
		fi
		AC_SUBST(FSC)
	else
		AM_CONDITIONAL(ENABLE_BLUETOOTH, false)
	fi
])

