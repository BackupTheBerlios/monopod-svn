SUBDIRS = entagged-sharp Monopod data po

EXTRA_DIST = \
	TODO	\
	intltool-extract.in     \
	intltool-merge.in       \
	intltool-update.in

dist-hook:
	grep -n TODO $(srcdir)/Monopod/*.cs >>$(distdir)/TODO || /bin/true
	if test -e doap.rdf.in; then \
		perl -pe 's/\@PACKAGE\@/$(PACKAGE)/g; \
			s/\@VERSION\@/$(VERSION)/g; \
			s/\@DISTDATE\@/$(shell date -u +%Y-%m-%d)/g;' \
			< doap.rdf.in > doap.rdf; \
		cp doap.rdf $(distdir)/; \
	fi


DISTCLEANFILES = \
	intltool-extract	\
	intltool-merge		\
	intltool-update		\
	doap.rdf
