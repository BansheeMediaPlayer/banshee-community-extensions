# BCE_ARG_DISABLE(ExtensionName, default)
# Sets enable_ExtensionName depending on the corresponding configuration flag :
# --enable-extensionname  : yes
# --disable-extensionname : no
# No flag : default
AC_DEFUN([BCE_ARG_DISABLE],
[
	AC_ARG_ENABLE([m4_tolower($1)],
		AS_HELP_STRING(m4_join([-], [--disable], m4_tolower($1)),
                                [Do not build the $1 extension]),
		[AS_TR_SH([enable_$1])=$enableval], [AS_TR_SH([enable_$1])=$2]
	)
])

# BCE_CHECK_EXTENSION_DEP(ExtensionName, VARIABLE-PREFIX, MODULES, ERROR-MESSAGE-IF-NOT-FOUND)
# Checks whether modules exist by calling PKG_CHECK_MODULES
# If they don't and the extension is explicitly enabled, write the error message
# and stop
AC_DEFUN([BCE_CHECK_EXTENSION_DEP],
[
	PKG_CHECK_MODULES($2,
		$3,
		[AS_TR_SH([have_$2])=yes],
		[AS_TR_SH([have_$2])=no])
	AC_SUBST($2[]_LIBS)
	AC_SUBST($2[]_CFLAGS)

	if test "x$AS_TR_SH([enable_$1])" = "xyes" -a "x$have_$2" = "xno"; then
		AC_MSG_ERROR([$4])
	fi
])
