AC_DEFUN([BCE_STREAMRECORDER],
[
	BCE_ARG_DISABLE([StreamRecorder], [no])

	if test "x$enable_StreamRecorder" = "xyes"; then
		AM_CONDITIONAL(ENABLE_STREAMRECORDER, true)
	else
		AM_CONDITIONAL(ENABLE_STREAMRECORDER, false)
	fi
])

