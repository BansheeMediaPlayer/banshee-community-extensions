AC_DEFUN([BCE_SOCIAL],
[
	BCE_ARG_DISABLE([Social], [try])

	BCE_CHECK_EXTENSION_DEP([Social], [LIBGWIBBERSHARP],
		[gwibber-gtk-sharp-0.0 >= 0.0.4 ],
		[gwibber-gtk-sharp was not found. Please install it or disable the Social extension by passing --disable-social])

	if test "x$enable_Social" = "xtry" \
		&& test "x$have_LIBGWIBBERSHARP" = "xyes"; then
		enable_Social=yes
	fi

	if test "x$enable_Social" = "xyes"; then
		AM_CONDITIONAL(ENABLE_SOCIAL, true)
	else
		enable_Social=no
		AM_CONDITIONAL(ENABLE_SOCIAL, false)
	fi
])
