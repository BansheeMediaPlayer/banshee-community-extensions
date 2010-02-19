AC_DEFUN([BCE_LIRC],
[
	BCE_ARG_DISABLE([Lirc], [try])

	AC_CHECK_HEADER(lirc/lirc_client.h,
		[AC_CHECK_LIB(lirc_client, lirc_init, have_lirclib=yes, have_lirclib=no)], have_lirclib=no)

	if test "x$enable_Lirc" = "xyes" -a "x$have_lirclib" = "xno"; then
		AC_MSG_ERROR([The lirc library was not found. Please install it or disable the Lirc extension by passing --disable-lirc])
	fi

	if test "x$enable_Lirc" = "xtry" \
		&& test "x$have_lirclib" = "xyes"; then
		enable_Lirc=yes
	fi

	if test "x$enable_Lirc" = "xyes"; then
		AM_CONDITIONAL(ENABLE_LIRC, true)
	else
		enable_Lirc=no
		AM_CONDITIONAL(ENABLE_LIRC, false)
	fi
])
