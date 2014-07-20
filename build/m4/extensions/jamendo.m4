AC_DEFUN([BCE_JAMENDO],
[
	BCE_ARG_DISABLE([Jamendo], [yes])

	BCE_CHECK_EXTENSION_DEP([Jamendo], [BANSHEE_WEBBROWSER],
		[banshee-webbrowser],
		[banshee-webbrowser was not found. Please install it or disable the Jamendo extension by passing --disable-jamendo])

	if test "x$enable_Jamendo" = "xtry" \
		&& test "x$have_BANSHEE_WEBBROWSER" = "xyes"; then
		enable_Jamendo=yes
	fi

	if test "x$enable_Jamendo" = "xyes"; then
		AM_CONDITIONAL(ENABLE_JAMENDO, true)
	else
		AM_CONDITIONAL(ENABLE_JAMENDO, false)
	fi
])

