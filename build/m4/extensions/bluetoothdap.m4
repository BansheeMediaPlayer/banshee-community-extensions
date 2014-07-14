AC_DEFUN([BCE_BLUETOOTHDAP],
[
	BCE_ARG_DISABLE([BluetoothDap], [yes])

	if test "x$enable_BluetoothDap" = "xyes"; then
		AM_CONDITIONAL(ENABLE_BLUETOOTHDAP, true)

        AC_PATH_PROG(FSC, fsharpc, no)
        if test "x$FSC" = "xno"; then
            AC_MSG_ERROR([You need to install an F# compiler.'])
        fi
        AC_SUBST(FSC)
	else
		AM_CONDITIONAL(ENABLE_BLUETOOTHDAP, false)
	fi
])

