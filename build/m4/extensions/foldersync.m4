AC_DEFUN([BCE_FOLDERSYNC],
[
	BCE_ARG_DISABLE([FolderSync], [yes])

	if test "x$enable_FolderSync" = "xyes"; then
		AM_CONDITIONAL(ENABLE_FOLDERSYNC, true)
	else
		AM_CONDITIONAL(ENABLE_FOLDERSYNC, false)
	fi
])

