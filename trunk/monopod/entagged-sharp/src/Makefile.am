MCS_FLAGS = -debug
ASSEMBLY_NAME = entagged-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

ASSEMBLY_SOURCES = \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/Ape/ApeTag.cs \
	$(srcdir)/Ape/MonkeyFileReader.cs \
	$(srcdir)/Ape/Util/ApeTagBinaryField.cs \
	$(srcdir)/Ape/Util/ApeTagField.cs \
	$(srcdir)/Ape/Util/ApeTagReader.cs \
	$(srcdir)/Ape/Util/ApeTagTextField.cs \
	$(srcdir)/Ape/Util/MonkeyDescriptor.cs \
	$(srcdir)/Ape/Util/MonkeyHeader.cs \
	$(srcdir)/Ape/Util/MonkeyInfoReader.cs \
	$(srcdir)/Ape/Util/WavFormatHeader.cs \
	$(srcdir)/Ape/Util/WavRIFFHeader.cs \
	$(srcdir)/Exceptions/CannotReadException.cs \
	$(srcdir)/Flac/FlacFileReader.cs \
	$(srcdir)/Flac/Util/FlacInfoReader.cs \
	$(srcdir)/Flac/Util/FlacTagReader.cs \
	$(srcdir)/Flac/Util/MetadataBlockDataStreamInfo.cs \
	$(srcdir)/Flac/Util/MetadataBlockHeader.cs \
	$(srcdir)/Util/AudioFileReader.cs \
	$(srcdir)/Util/ByteBuffer.cs \
	$(srcdir)/Util/TagField.cs \
	$(srcdir)/Util/Utils.cs \
	$(srcdir)/Mp3/Id3Tag.cs \
	$(srcdir)/Mp3/Mp3FileReader.cs \
	$(srcdir)/Mp3/Util/Id3frames/CommId3Frame.cs \
	$(srcdir)/Mp3/Util/Id3frames/GenericId3Frame.cs \
	$(srcdir)/Mp3/Util/Id3frames/Id3Frame.cs \
	$(srcdir)/Mp3/Util/Id3frames/TextId3Frame.cs \
	$(srcdir)/Mp3/Util/Id3frames/ApicId3Frame.cs \
	$(srcdir)/Mp3/Util/Id3v1TagReader.cs \
	$(srcdir)/Mp3/Util/Id3v23TagReader.cs \
	$(srcdir)/Mp3/Util/Id3v2TagReader.cs \
	$(srcdir)/Mp3/Util/Id3v2TagSynchronizer.cs \
	$(srcdir)/Mp3/Util/LameMPEGFrame.cs \
	$(srcdir)/Mp3/Util/Mp3InfoReader.cs \
	$(srcdir)/Mp3/Util/MPEGFrame.cs \
	$(srcdir)/Mp3/Util/XingMPEGFrame.cs \
	$(srcdir)/Mpc/MpcFileReader.cs \
	$(srcdir)/Mpc/Util/MpcHeader.cs \
	$(srcdir)/Mpc/Util/MpcInfoReader.cs \
	$(srcdir)/Ogg/OggFileReader.cs \
	$(srcdir)/Ogg/OggTag.cs \
	$(srcdir)/Ogg/Util/OggInfoReader.cs \
	$(srcdir)/Ogg/Util/OggPageHeader.cs \
	$(srcdir)/Ogg/Util/OggTagField.cs \
	$(srcdir)/Ogg/Util/OggTagReader.cs \
	$(srcdir)/Ogg/Util/VorbisCodecHeader.cs \
	$(srcdir)/Ogg/Util/VorbisTagReader.cs \
	$(srcdir)/M4a/M4aFileReader.cs \
	$(srcdir)/M4a/M4aTag.cs \
	$(srcdir)/M4a/Util/M4aInfoReader.cs \
	$(srcdir)/M4a/Util/M4aTagField.cs \
	$(srcdir)/M4a/Util/M4aTagReader.cs \
	$(srcdir)/Tag.cs \
	$(srcdir)/AudioFile.cs \
	$(srcdir)/AudioFileIO.cs \
	$(srcdir)/EncodingInfo.cs \
	$(srcdir)/AudioFileWrapper.cs 

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -keyfile:$(top_srcdir)/entagged-sharp.snk -target:library -out:$@ $(ASSEMBLY_SOURCES)

# NOT FOR GAC USE UNTIL MUCH ADOPTION
# http://www.mono-project.com/Guidelines:Application_Deployment
#
#install-data-hook:
#	$(GACUTIL) /i $(ASSEMBLY) /package $(PACKAGE) /gacdir $(libdir) /root $(DESTDIR)$(libdir)

#uninstall-hook:
#	$(GACUTIL) /u $(ASSEMBLY_NAME) /package $(PACKAGE) /gacdir $(libdir) /root $(DESTDIR)$(libdir)

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	AssemblyInfo.cs.in

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = AssemblyInfo.cs *.mdb Makefile.in *.dll *.exe

