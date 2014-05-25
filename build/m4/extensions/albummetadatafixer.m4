AC_DEFUN([BCE_ALBUMMETADATAFIXER],
[
	BCE_ARG_DISABLE([AlbumMetadataFixer], [yes])

	if test "x$enable_AlbumMetadataFixer" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ALBUMMETADATAFIXER, true)
	else
		AM_CONDITIONAL(ENABLE_ALBUMMETADATAFIXER, false)
	fi
])

