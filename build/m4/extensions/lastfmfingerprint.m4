AC_DEFUN([BCE_LASTFMFINGERPRINT],
[
	AC_REQUIRE([AC_PROG_CXX])
	BCE_ARG_DISABLE([LastfmFingerprint], [try])

	BCE_CHECK_EXTENSION_DEP([LastfmFingerprint], [GLIB],
		[glib-2.0],
		[The glib library was not found. Please install it or disable the LastfmFingerprint extension by passing --disable-lastfmfingerprint])

	BCE_CHECK_EXTENSION_DEP([LastfmFingerprint], [FFTW3F],
		[fftw3f],
		[The fftw3f library was not found. Please install it or disable the LastfmFingerprint extension by passing --disable-lastfmfingerprint])

	BCE_CHECK_EXTENSION_DEP([LastfmFingerprint], [LIBSAMPLERATE],
		[samplerate],
		[The samplerate library was not found. Please install it or disable the LastfmFingerprint extension by passing --disable-lastfmfingerprint])

	BCE_CHECK_EXTENSION_DEP([LastfmFingerprint], [BANSHEE_LASTFM],
		[banshee-lastfm],
		[banshee-lastfm was not found. Please install it or disable the LastfmFingerprint extension by passing --disable-lastfmfingerprint])

	GSTREAMER_REQUIRED_VERSION=0.10.15
	BCE_CHECK_EXTENSION_DEP([LastfmFingerprint], [GSTREAMER],
		[gstreamer-0.10 >= $GSTREAMER_REQUIRED_VERSION
		 gstreamer-base-0.10 >= $GSTREAMER_REQUIRED_VERSION
		 gstreamer-plugins-base-0.10 >= $GSTREAMER_REQUIRED_VERSION],
		[GStreamer >= $GSTREAMER_REQUIRED_VERSION not found. Please install it or disable the LastfmFingerprint extension by passing --disable-lastfmfingerprint])

	if test "x$enable_LastfmFingerprint" = "xtry" \
		&& test "x$have_GLIB" = "xyes" \
		&& test "x$have_FFTW3F" = "xyes" \
		&& test "x$have_LIBSAMPLERATE" = "xyes" \
		&& test "x$have_BANSHEE_LASTFM" = "xyes" \
		&& test "x$have_GSTREAMER" = "xyes"; then
		enable_LastfmFingerprint=yes
	fi

	if test "x$enable_LastfmFingerprint" = "xyes"; then
		AM_CONDITIONAL(ENABLE_LASTFMFINGERPRINT, true)
	else
		enable_LastfmFingerprint=no
		AM_CONDITIONAL(ENABLE_LASTFMFINGERPRINT, false)
	fi
])
