monowrapdir = $(bindir)
monowrap_SCRIPTS = monopod

entagged_dll = entagged-sharp.dll

monopoddir = $(pkglibdir)
monopod_SCRIPTS = Monopod.exe $(entagged_dll)

$(entagged_dll): $(top_builddir)/entagged-sharp/src/entagged-sharp.dll
	cp $(top_builddir)/entagged-sharp/src/entagged-sharp.dll $(entagged_dll)

EXTRA_DIST = $(monopod_sources) $(monopod_sources_in) \
	     monopod.in $(monopod_resources)

CLEANFILES = Monopod.exe entagged-sharp.dll
DISTCLEANFILES = Monopod $(monopod_generated_sources)

OTHERLIBS = -r:System.Data -r:System.Xml -r:System.Web \
	-r:Mono.Data.SqliteClient -r:Mono.Posix

monopod_sources_in = AssemblyInfo.cs.in Defines.cs.in
monopod_generated_sources = $(monopod_sources_in:.in=)
monopod_sources = AddChannelWindow.cs Cast.cs Channel.cs \
	ChannelWindow.cs Dialogs.cs \
	ItemList.cs Main.cs MyWindow.cs SearchEntry.cs \
	SqlUtil.cs Store.cs StringUtils.cs TrayLib.cs \
	WebUtil.cs IPod.cs IPodChoose.cs

monopod_build_sources = $(addprefix $(srcdir)/, $(monopod_sources))
monopod_build_sources += $(monopod_generated_sources)

monopod_resources = $(srcdir)/podcast.png $(srcdir)/AddWindow.glade \
	$(srcdir)/NewChannel.glade $(srcdir)/IPodWindow.glade \
	$(srcdir)/DeleteFiles.glade $(srcdir)/ErrorDialog.glade

monopod_embedded = $(foreach res,$(monopod_resources),\
	$(addprefix -resource:,$(res)),$(notdir $(res)))

Monopod.exe: $(monopod_build_sources) $(monopod_resources) $(entagged_dll)
	$(MCS) $(GTKSHARP_LIBS) $(GLADESHARP_LIBS) \
	$(GCONFSHARP_LIBS) $(IPODSHARP_LIBS) \
	-r:$(entagged_dll) \
	$(GNOMESHARP_LIBS) $(OTHERLIBS) $(monopod_embedded) \
	$(MCS_FLAGS) \
	 -out:$@ \
	$(monopod_build_sources)

