AC_DEFUN([BCE_ALBUMARTWRITER],
[
	BCE_ARG_DISABLE([AlbumArtWriter], [yes])

	if test "x$enable_AlbumArtWriter" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ALBUMARTWRITER, true)
	else
		AM_CONDITIONAL(ENABLE_ALBUMARTWRITER, false)
	fi
])

