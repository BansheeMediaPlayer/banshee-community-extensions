AC_DEFUN([BCE_APPINDICATOR],
[
	BCE_ARG_DISABLE([AppIndicator], [try])

	BCE_CHECK_EXTENSION_DEP([AppIndicator], [APPINDICATORSHARP],
		[appindicator-sharp-0.1],
		[AppIndicator-sharp was not found. Please install it or disable the AppIndicator extension by passing --disable-appindicator])

	BCE_CHECK_EXTENSION_DEP([AppIndicator], [NOTIFYSHARP],
		[notify-sharp],
		[Notify-sharp was not found. Please install it or disable the AppIndicator extension by passing --disable-appindicator])

	if test "x$enable_AppIndicator" = "xtry" \
		&& test "x$have_APPINDICATORSHARP" = "xyes" \
		&& test "x$have_NOTIFYSHARP" = "xyes"; then
		enable_AppIndicator=yes
	fi

	if test "x$enable_AppIndicator" = "xyes"; then
		AM_CONDITIONAL(ENABLE_APPINDICATOR, true)
	else
		enable_AppIndicator=no
		AM_CONDITIONAL(ENABLE_APPINDICATOR, false)
	fi
])
