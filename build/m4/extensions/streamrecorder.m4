AC_DEFUN([BCE_STREAMRECORDER],
[
	BCE_ARG_DISABLE([StreamRecorder], [yes])

	if test "x$enable_StreamRecorder" = "xyes"; then
		SHAMROCK_FIND_PROGRAM_OR_BAIL(SED, sed)
 		OLD_GLIB="`$PKG_CONFIG --libs glib-sharp-2.0 | $SED -e 's/-r:/-r:oldGlib=/g'`"
                AC_SUBST(OLD_GLIB)
		AM_CONDITIONAL(ENABLE_STREAMRECORDER, true)
	else
		AM_CONDITIONAL(ENABLE_STREAMRECORDER, false)
	fi
])

