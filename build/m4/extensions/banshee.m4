AC_DEFUN([BCE_FIND_BANSHEE],
[
	BANSHEE_LIBDIR=`$PKG_CONFIG banshee-1-core --variable=bansheedir`
	SYSTEM_EXTENSIONS=$BANSHEE_LIBDIR/Extensions
	REL_EXTENSIONS_DIR=`basename $SYSTEM_EXTENSIONS`
	REL_BANSHEE_DIR=`echo "$SYSTEM_EXTENSIONS" | sed -e "s|\/$REL_EXTENSIONS_DIR||"`
	REL_BANSHEE_DIR=`basename $REL_BANSHEE_DIR`
	REL_EXTENSIONS_DIR=$REL_BANSHEE_DIR/$REL_EXTENSIONS_DIR

	EXTENSION_DIR=$libdir/$REL_EXTENSIONS_DIR
	AC_SUBST(EXTENSION_DIR)

	expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
			case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
			eval echo $libdir )`
	expanded_extensionsdir=$expanded_libdir/$REL_EXTENSIONS_DIR

])
