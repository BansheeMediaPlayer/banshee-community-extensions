AC_DEFUN([BCE_FIND_BANSHEE],
[
	BANSHEE_LIBDIR=`$PKG_CONFIG banshee-core --variable=bansheedir`
	BANSHEE_EXTDIR=$BANSHEE_LIBDIR/Extensions
	REL_EXTENSIONS_DIR=`basename $BANSHEE_EXTDIR`
	REL_BANSHEE_DIR=`echo "$BANSHEE_EXTDIR" | sed -e "s|\/$REL_EXTENSIONS_DIR||"`
	REL_BANSHEE_DIR=`basename $REL_BANSHEE_DIR`
	REL_EXTENSIONS_DIR=$REL_BANSHEE_DIR/$REL_EXTENSIONS_DIR

	EXTENSION_DIR=$libdir/$REL_EXTENSIONS_DIR
	AC_SUBST(EXTENSION_DIR)
	AC_SUBST(BANSHEE_LIBDIR)
	AC_SUBST(BANSHEE_EXTDIR)

	expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
			case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
			eval echo $libdir )`
	expanded_extensionsdir=$expanded_libdir/$REL_EXTENSIONS_DIR

])
