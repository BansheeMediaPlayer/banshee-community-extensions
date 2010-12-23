AC_DEFUN([BCE_ZEITGEISTDP],
[
	BCE_ARG_DISABLE([ZeitgeistDataprovider], [try])

	BCE_CHECK_EXTENSION_DEP([ZeitgeistDataprovider], [ZEITGEIST_SHARP],
		[zeitgeist-sharp],
		[zeitgeist-sharp was not found. Please install it or disable the ZeitgeistDp extension by passing --disable-zeitgeistdataprovider])

	if test "x$enable_ZeitgeistDataprovider" = "xtry" \
		&& test "x$have_ZEITGEIST_SHARP" = "xyes"; then
		enable_ZeitgeistDataprovider=yes
	fi

	if test "x$enable_ZeitgeistDataprovider" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ZEITGEISTDATAPROVIDER, true)
	else
		enable_ZeitgeistDataprovider=no
		AM_CONDITIONAL(ENABLE_ZEITGEISTDATAPROVIDER, false)
	fi
])

