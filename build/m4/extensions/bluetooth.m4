AC_DEFUN([BCE_BLUETOOTH],
[
	BCE_ARG_DISABLE([Bluetooth], [yes])

	if test "x$enable_Bluetooth" = "xyes"; then
		AM_CONDITIONAL(ENABLE_BLUETOOTH, true)
		SHAMROCK_FIND_FSHARP_COMPILER
	else
		AM_CONDITIONAL(ENABLE_BLUETOOTH, false)
	fi
])

