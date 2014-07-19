AC_DEFUN([BCE_ALARMCLOCK],
[
	BCE_ARG_DISABLE([AlarmClock], [no])

	if test "x$enable_AlarmClock" = "xyes"; then
		AM_CONDITIONAL(ENABLE_ALARMCLOCK, true)
	else
		AM_CONDITIONAL(ENABLE_ALARMCLOCK, false)
	fi
])

