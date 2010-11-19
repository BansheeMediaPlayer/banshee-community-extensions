AC_DEFUN([BCE_ZEITGEISTDP],
[
	BCE_ARG_DISABLE([ZeitgeistDp], [try])

	BCE_CHECK_EXTENSION_DEP([ZeitgeistDp], [ZEITGEIST_SHARP],
		[zeitgeist-sharp],
		[zeitgeist-sharp was not found. Please install it or disable the ZeitgeistDp extension by passing --disable-zeitgeistdp])

	if test "x$enable_ZeitgeistDp" = "xtry" \
		&& test "x$have_ZEITGEIST_SHARP" = "xyes"; then
		enable_ZeitgeistDp=yes
	fi

	if test "x$enable_ZeitgeistDp" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ZEITGEISTDP, true)
	else
		AM_CONDITIONAL(ENABLE_ZEITGEISTDP, false)
	fi
])

