AC_DEFUN([BCE_VOICECONTROL],
[
	BCE_ARG_DISABLE([VoiceControl], [try])

	BCE_CHECK_EXTENSION_DEP([VoiceControl], [GLIB],
		[glib-2.0],
		[The glib library was not found. Please install it or disable the VoiceControl extension by passing --disable-voicecontrol])

	GSTREAMER_REQUIRED_VERSION=0.10.15
	BCE_CHECK_EXTENSION_DEP([VoiceControl], [GSTREAMER],
		[gstreamer-0.10 >= $GSTREAMER_REQUIRED_VERSION
		 gstreamer-base-0.10 >= $GSTREAMER_REQUIRED_VERSION
		 gstreamer-plugins-base-0.10 >= $GSTREAMER_REQUIRED_VERSION],
		[GStreamer >= $GSTREAMER_REQUIRED_VERSION not found. Please install it or disable the VoiceControl extension by passing --disable-voicecontrol])

	if test "x$enable_VoiceControl" = "xtry" \
		&& test "x$have_GLIB" = "xyes" \
		&& test "x$have_GSTREAMER" = "xyes"; then
		enable_VoiceControl=yes
	fi

	if test "x$enable_VoiceControl" = "xyes"; then
		AM_CONDITIONAL(ENABLE_VOICECONTROL, true)
	else
		enable_VoiceControl=no
		AM_CONDITIONAL(ENABLE_VOICECONTROL, false)
	fi
])
