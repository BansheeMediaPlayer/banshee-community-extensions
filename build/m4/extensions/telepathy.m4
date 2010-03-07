AC_DEFUN([BCE_TELEPATHY],
[
	BCE_ARG_DISABLE([Telepathy], [try])

	BCE_CHECK_EXTENSION_DEP([Telepathy], [NOTIFY_SHARP],
		[notify-sharp],
		[notify-sharp was not found. Please install notify-sharp or disable the Telepathy extension by passing --disable-telepathy])

	BCE_CHECK_EXTENSION_DEP([Telepathy], [MONO],
		[mono >= 2.4.2],
		[Mono >= 2.4.2 was not found. Please install it or disable the Telepathy extension by passing --disable-telepathy])

	if test "x$enable_Telepathy" = "xtry" \
		&& test "x$have_NOTIFY_SHARP" = "xyes" \
		&& test "x$have_MONO" = "xyes"; then
		enable_Telepathy=yes
	fi

	if test "x$enable_Telepathy" = "xyes"; then
		AM_CONDITIONAL(ENABLE_TELEPATHY, true)
	else
		enable_Telepathy=no
		AM_CONDITIONAL(ENABLE_TELEPATHY, false)
	fi
])

