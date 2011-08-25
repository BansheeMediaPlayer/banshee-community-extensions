AC_DEFUN([BCE_DUPLICATESONGDETECTOR],
[
	BCE_ARG_DISABLE([DuplicateSongDetector], [yes])

	if test "x$enable_DuplicateSongDetector" = "xyes"; then
		AM_CONDITIONAL(ENABLE_DUPLICATESONGDETECTOR, true)
	else
		AM_CONDITIONAL(ENABLE_DUPLICATESONGDETECTOR, false)
	fi
])

