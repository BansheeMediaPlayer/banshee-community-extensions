# This must be called after BCE_STREAMRECORDER, because it depends on it
AC_DEFUN([BCE_KARAOKE],
[
	BCE_ARG_DISABLE([Karaoke], [yes])

	if test "x$enable_Karaoke" = "xyes" \
		&& test "x$enable_StreamRecorder" = "xno"; then
		AC_MSG_ERROR([The Karaoke extension requires the StreamRecorder extension. Please enable the StreamRecorder extension or disable the Karaoke extensions by passing --disable-karaoke])
	fi

	if test "x$enable_Karaoke" = "xtry"; then
		enable_Karaoke=yes
	fi

	if test "x$enable_Karaoke" = "xyes"; then
		AM_CONDITIONAL(ENABLE_KARAOKE, true)
	else
		AM_CONDITIONAL(ENABLE_KARAOKE, false)
	fi
])

