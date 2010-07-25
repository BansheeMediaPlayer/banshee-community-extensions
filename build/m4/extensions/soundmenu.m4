AC_DEFUN([BCE_SOUNDMENU],
[
	BCE_ARG_DISABLE([SoundMenu], [try])

        BCE_CHECK_EXTENSION_DEP([SoundMenu], [BANSHEE_160],
                [banshee-1-thickclient >= 1.6.0],
                [Banshee 1.6.0 was not found. Please install it or disable the SoundMenu extension by passing --disable-soundmenu])

	BCE_CHECK_EXTENSION_DEP([SoundMenu], [INDICATESHARP],
		[indicate-sharp-0.1 >= 0.4.1],
		[indicate-sharp was not found. Please install it or disable the SoundMenu extension by passing --disable-soundmenu])

	BCE_CHECK_EXTENSION_DEP([SoundMenu], [NOTIFYSHARP],
		[notify-sharp],
		[Notify-sharp was not found. Please install it or disable the SoundMenu extension by passing --disable-soundmenu])

	if test "x$enable_SoundMenu" = "xtry" \
		&& test "x$have_BANSHEE_160" = "xyes" \
		&& test "x$have_INDICATESHARP" = "xyes" \
		&& test "x$have_NOTIFYSHARP" = "xyes"; then
		enable_SoundMenu=yes
	fi

	if test "x$enable_SoundMenu" = "xyes"; then
		AM_CONDITIONAL(ENABLE_SOUNDMENU, true)
	else
		enable_SoundMenu=no
		AM_CONDITIONAL(ENABLE_SOUNDMENU, false)
	fi
])

