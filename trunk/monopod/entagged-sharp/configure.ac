AC_INIT(README)
AC_CANONICAL_SYSTEM
AM_MAINTAINER_MODE

MAJOR_VERSION=0
MINOR_VERSION=1
MICRO_VERSION=2
VERSION=$MAJOR_VERSION.$MINOR_VERSION.$MICRO_VERSION

AM_INIT_AUTOMAKE(entagged-sharp, $VERSION)

AC_SUBST(MAJOR_VERSION)
AC_SUBST(MINOR_VERSION)
AC_SUBST(MICRO_VERSION)

AC_PROG_INSTALL

AC_DEFINE_UNQUOTED(MAJOR_VERSION, $MAJOR_VERSION, [Major version number])
AC_DEFINE_UNQUOTED(MINOR_VERSION, $MINOR_VERSION, [Minor version number])
AC_DEFINE_UNQUOTED(MICRO_VERSION, $MICRO_VERSION, [Micro version number])

dnl Mono
AC_PATH_PROG(MONO, mono)
if test "x$MONO" = "x"; then
	AC_MSG_ERROR([Can not find "mono" in your PATH])
fi

dnl mcs
AC_PATH_PROG(MCS, mcs)
if test "x$MCS" = "x" ; then
	AC_MSG_ERROR([Can not find "mcs" in your PATH])
fi

dnl gacutil
AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno"; then
	AC_MSG_ERROR([You need to install gacutil])
fi

AC_OUTPUT([
entagged-sharp.pc
Makefile
src/AssemblyInfo.cs
src/Makefile
])

