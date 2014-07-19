AC_DEFUN([BCE_ARTISTLISTCOVERS],
[
	BCE_ARG_DISABLE([ArtistListCovers], [no])

	if test "x$enable_ArtistListCovers" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ARTISTLISTCOVERS, true)
	else
		AM_CONDITIONAL(ENABLE_ARTISTLISTCOVERS, false)
	fi
])

