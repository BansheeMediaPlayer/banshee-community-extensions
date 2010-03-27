AC_DEFUN([BCE_APPINDICATOR],
[
	BCE_ARG_DISABLE([AppIndicator], [yes])

	BCE_CHECK_EXTENSION_DEP([AppIndicator], [APPINDICATORSHARP],
		[appindicator-sharp-0.1],
		[AppIndicator-sharp was not found. Please install it or disable the AppIndicator extension by passing --disable-appindicator])

	BCE_CHECK_EXTENSION_DEP([AppIndicator], [NOTIFYSHARP],
		[notify-sharp],
		[Notify-sharp was not found. Please install it or disable the AppIndicator extension by passing --disable-appindicator])

	if test "x$enable_AppIndicator" = "xyes"; then
		AM_CONDITIONAL(ENABLE_APPINDICATOR, true)
	else
		AM_CONDITIONAL(ENABLE_APPINDICATOR, false)
	fi
])
