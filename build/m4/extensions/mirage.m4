AC_DEFUN([BCE_MIRAGE],
[
	BCE_ARG_DISABLE([Mirage], [try])

	BCE_CHECK_EXTENSION_DEP([Mirage], [GLIB],
		[glib-2.0],
		[The glib library was not found. Please install it or disable the Mirage extension by passing --disable-mirage])

	BCE_CHECK_EXTENSION_DEP([Mirage], [FFTW3F],
		[fftw3f],
		[The fftw3f library was not found. Please install it or disable the Mirage extension by passing --disable-mirage])

	BCE_CHECK_EXTENSION_DEP([Mirage], [LIBSAMPLERATE],
		[samplerate],
		[The samplerate library was not found. Please install it or disable the Mirage extension by passing --disable-mirage])

	GSTREAMER_REQUIRED_VERSION=0.10.15
	BCE_CHECK_EXTENSION_DEP([Mirage], [GSTREAMER],
		[gstreamer-0.10 >= $GSTREAMER_REQUIRED_VERSION
		 gstreamer-base-0.10 >= $GSTREAMER_REQUIRED_VERSION
		 gstreamer-plugins-base-0.10 >= $GSTREAMER_REQUIRED_VERSION],
		[GStreamer >= $GSTREAMER_REQUIRED_VERSION not found. Please install it or disable the Mirage extension by passing --disable-mirage])

	if test "x$enable_Mirage" = "xtry" \
		&& test "x$have_GLIB" = "xyes" \
		&& test "x$have_FFTW3F" = "xyes" \
		&& test "x$have_LIBSAMPLERATE" = "xyes" \
		&& test "x$have_GSTREAMER" = "xyes"; then
		enable_Mirage=yes
	fi

	if test "x$enable_Mirage" = "xyes"; then
		AM_CONDITIONAL(ENABLE_MIRAGE, true)
	else
		enable_Mirage=no
		AM_CONDITIONAL(ENABLE_MIRAGE, false)
	fi
])
