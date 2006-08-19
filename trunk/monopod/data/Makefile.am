@INTLTOOL_DESKTOP_RULE@

desktop_in_files = monopod.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

DISTCLEANFILES = \
        $(desktop_DATA)

pixmapdir = $(datadir)/pixmaps
pixmap_DATA = monopod.png

EXTRA_DIST = \
        $(desktop_in_files) \
	$(pixmap_DATA)

