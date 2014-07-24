AC_DEFUN([BCE_RADIOSTATIONFETCHER],
[
	BCE_ARG_DISABLE([RadioStationFetcher], [yes])

	if test "x$enable_RadioStationFetcher" = "xyes"; then
		AM_CONDITIONAL(ENABLE_RADIOSTATIONFETCHER, true)
	else
		AM_CONDITIONAL(ENABLE_RADIOSTATIONFETCHER, false)
	fi
])

