AC_INIT(Monopod, 0.5, [https://developer.berlios.de/bugs/?group_id=7191])
AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)
AM_INIT_AUTOMAKE
AM_MAINTAINER_MODE
AC_PROG_INTLTOOL([0.21])

dnl AC_PROG_CC
dnl AC_ISC_POSIX
dnl AC_HEADER_STDC

AM_PROG_LIBTOOL

AC_PROG_INSTALL

GNOME_DEBUG_CHECK
GNOME_COMPILE_WARNINGS(error)
AM_PATH_GLIB_2_0

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

MONO_REQUIRED_VERSION=1.1.6
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)

MONODIR=`$PKG_CONFIG --variable=exec_prefix mono`
MONOBIN="$MONODIR/bin"
MONO="$MONOBIN/mono"
MINT="$MONOBIN/mint"
MCS="$MONOBIN/mcs"
GACUTIL="$MONOBIN/gacutil"

if test ! -x "$MONO" -a ! -x "$MINT"; then
	AC_MSG_ERROR([No Mono runtime found])
fi

if test ! -x "$MONO"; then
    MONO = $MINT
fi

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS compiler found])
fi

if test ! -x "$GACUTIL"; then
	AC_MSG_ERROR([No Mono gacutil found])
fi

AC_SUBST(MCS)
AC_SUBST(MONO)
AC_SUBST(GACUTIL)

MCS_FLAGS="-codepage:utf8"
MONO_FLAGS=
if test $USE_MAINTAINER_MODE = yes; then
        MCS_FLAGS="$MCS_FLAGS -debug"
        MONO_FLAGS="$MONO_FLAGS --debug"
fi
AC_SUBST(MCS_FLAGS)
AC_SUBST(MONO_FLAGS)

dnl gtk-sharp
GTKSHARP_REQUIRED_VERSION=2.8.0
PKG_CHECK_MODULES(GTKSHARP, gtk-sharp-2.0 >= $GTKSHARP_REQUIRED_VERSION)
AC_SUBST(GTKSHARP_LIBS)

dnl glade-sharp
GLADESHARP_REQUIRED_VERSION=2.8.0
PKG_CHECK_MODULES(GLADESHARP, glade-sharp-2.0 >= $GLADESHARP_REQUIRED_VERSION)
AC_SUBST(GLADESHARP_LIBS)

dnl gconf-sharp
GCONFSHARP_REQUIRED_VERSION=2.8.0
PKG_CHECK_MODULES(GCONFSHARP, gconf-sharp-2.0 >= $GCONFSHARP_REQUIRED_VERSION)
AC_SUBST(GCONFSHARP_LIBS)

dnl ipod-sharp

dnl configure options
AC_ARG_ENABLE(ipod, AC_HELP_STRING([--enable-ipod], [Enable iPod support]), , enable_ipod="yes")
enable_ipodsharp="${enable_ipod}"

dnl DBus devel detect .so version for dll maps
PKG_CHECK_MODULES(DBUS, dbus-1 >= 0.60, dbus60=yes, dbus60=no)
if test "x$dbus60" = "xyes"; then
	DBUS_SOVERSION=2
else
	DBUS_SOVERSION=1
fi
AC_SUBST(DBUS_SOVERSION)

IPODSHARP_REQUIRED=0.5.15
PKG_CHECK_MODULES(IPODSHARP,
	ipod-sharp >= $IPODSHARP_REQUIRED ipod-sharp-ui >= $IPODSHARP_REQUIRED,
	enable_ipodsharp="$enable_ipodsharp", enable_ipodsharp=no)

if test "x$enable_ipodsharp" = "xyes"; then
	IPODSHARP_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries ipod-sharp` `$PKG_CONFIG --variable=Libraries ipod-sharp-ui`"
	AC_SUBST(IPODSHARP_ASSEMBLIES)
	AC_SUBST(IPODSHARP_LIBS)
fi
AM_CONDITIONAL(ENABLE_IPOD, test "x$enable_ipodsharp" = "xyes")

dnl i18n
ALL_LINGUAS="nl ru"
GETTEXT_PACKAGE=monopod
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE", [Gettext package])
AM_GLIB_GNU_GETTEXT

AC_CONFIG_SUBDIRS(entagged-sharp)

AC_OUTPUT([
Makefile
Monopod/Makefile
Monopod/AssemblyInfo.cs
Monopod/Defines.cs
Monopod/monopod
Monopod/ipod-sharp.dll.config
data/Makefile
po/Makefile.in
])
