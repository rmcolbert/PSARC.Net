using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace PSArcHandler
{
    public class PSARC : IDisposable
    {
        private EndianReader br;

        private readonly uint defaultBlockSize = 0x00010000; // 64KB

        public struct Header
        {
            public uint MagicNumber;        // 0x50534152 'PSAR'
            public uint VersionNumber;
            public uint CompressionMethod;  // zlib or lzma
            public uint TocLength;          // Includes Header
            public uint TocEntrySize;       // Size of a single entry in the TOC, currently 30. This allows the size to be expanded in the future while maintaining backward compat.
            public uint TocEntries;         // Total number of entries including the manifest
            public uint BlockSize;          // The size of each block decompressed.
            public uint ArchiveFlags;

            public Header(bool isDefault) : this()
            {
                if (isDefault)
                {
                    MagicNumber = 0x50534152;
                    VersionNumber = 0x00010004;
                    CompressionMethod = 0x7a6c6962;
                    BlockSize = 0x00010000; // 64KB
                    ArchiveFlags = 0x0;
                }
            }
        }
        public Header psHeader = new Header();

        public struct TOCEntry
        {
            public byte[] MD5;              // hash of FILENAME.
            public uint BlockListStart;     // Not used in this code for decompression but needs to be calculated on compression
            public ulong OriginalSize;      // UInt40 (5 Bytes)
            public ulong StartOffset;       // UInt40 (5 Bytes)
            public string FileName;

            public TOCEntry(byte[] MD5, uint blockListStart, ulong originalSize, ulong startOffset) : this()
            {
                this.MD5 = MD5;
                this.BlockListStart = blockListStart;
                this.OriginalSize = originalSize;
                this.StartOffset = startOffset;
            }

            public TOCEntry(TOCEntry tocEntry, string fileName)
            {
                this.MD5 = tocEntry.MD5;
                this.BlockListStart = tocEntry.BlockListStart;
                this.OriginalSize = tocEntry.OriginalSize;
                this.StartOffset = tocEntry.StartOffset;
                this.FileName = fileName;
            }
        }
        public List<TOCEntry> TOC = new List<TOCEntry>();
        private List<String> tocList = new List<string>();

        public uint DefaultBlockSize
        {
            get
            {
                return defaultBlockSize;
            }
        }

        public struct UnpackedFile
        {
            public string FileName;
            public byte[] BinaryFile;

            public UnpackedFile(string fileName, byte[] outFile) : this()
            {
                this.FileName = fileName;
                this.BinaryFile = outFile;
            }
        }

        public struct PackedFile
        {
            public TOCEntry TocEntry;
            public byte[] CompressedFile;

            public PackedFile(TOCEntry tocEntry, byte[] compressedFile) : this()
            {
                this.TocEntry = tocEntry;
                this.CompressedFile = compressedFile;
            }
        }

        public PSARC()
        {
        }

        public PSARC(String fileName)
        {
            LoadStream(new FileStream(fileName, FileMode.Open, FileAccess.Read));
        }

        public PSARC(FileStream archiveStream)
        {
            LoadStream(archiveStream);
        }

        public void Dispose()
        {
            if (br != null) { br.Close(); br.Dispose(); br = null; }
        }

        private void LoadStream(FileStream archiveStream)
        {
            if (br != null) { br.Close(); br.Dispose(); br = null; }
            br = new EndianReader(archiveStream, EndianType.BigEndian);
            ReadManifest();
        }

        ~PSARC()
        {
            if (br != null) { br.Close(); br.Dispose(); br = null; }
        }

        private void ReadHeader()
        {
            br.BaseStream.Position = 0;  // Rewind the stream just in case
            psHeader = new Header();
            psHeader.MagicNumber = br.ReadUInt32();
            psHeader.VersionNumber = br.ReadUInt32();
            psHeader.CompressionMethod = br.ReadUInt32();
            psHeader.TocLength = br.ReadUInt32();
            psHeader.TocEntrySize = br.ReadUInt32();
            psHeader.TocEntries = br.ReadUInt32();
            psHeader.BlockSize = br.ReadUInt32();
            psHeader.ArchiveFlags = br.ReadUInt32();
        }

        public List<TOCEntry> ReadManifest(string fileName)
        {
            LoadStream(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            return ReadManifest();
        }

        public List<TOCEntry> ReadManifest()
        {
            // Always reload the header before reading the manifest
            ReadHeader();

            uint i = 0;
            TOC = new List<TOCEntry>();
            for (i = 0; i < psHeader.TocEntries; i++)
                TOC.Add(new TOCEntry(br.ReadBytes(16), br.ReadUInt32(), br.ReadUInt40(), br.ReadUInt40()));

            // Extract the Manifest List
            UnpackedFile manifest = DecompressFile(0);
            tocList = new List<string>(System.Text.Encoding.Default.GetString(manifest.BinaryFile).Split('\n'));
            tocList.Insert(0, "manifest.txt");  // Insert an entry for the manifest name

            List<TOCEntry> tOCtmp = TOC;
            TOC = new List<TOCEntry>();
            for (i = 0; i < tOCtmp.Count; i++)
                TOC.Add(new TOCEntry(tOCtmp[(int)i], tocList[(int)i])); // Create a new TOC that includes the file name from the manifest

            tOCtmp = null;
            return TOC;
        }

        public UnpackedFile DecompressFile(int manifestLocation)
        {
            if (manifestLocation > (psHeader.TocEntries - 1)) return new UnpackedFile();

            byte[] outFile = null;
            br.BaseStream.Position = (long)TOC[manifestLocation].StartOffset; // From manifest

            UInt16 isZipped = br.ReadUInt16();
            br.BaseStream.Position -= 2;    // Rewind 2 bytes

            ulong cBlockSize = psHeader.BlockSize;

            // Calculate the number of blocks the uncompressed file would consume (this is more data than needed for decompression)
            //ulong zBlocks = ((TOC[manifestLocation].originalSize - (TOC[manifestLocation].originalSize % cBlockSize)) / cBlockSize)
            //               + (TOC[manifestLocation].originalSize % cBlockSize) == 0 ? 0u : 1u;

            ulong zBlocks = ((TOC[manifestLocation].OriginalSize - (TOC[manifestLocation].OriginalSize % cBlockSize)) / cBlockSize);
            if (TOC[manifestLocation].OriginalSize % cBlockSize > 0) zBlocks++;


            if (isZipped == 0x78da || isZipped == 0x7801) // Stream is compressed
            {
                ulong fileSize = zBlocks * cBlockSize;  // Only pass a part of the whole archive stream to be inflated.
                outFile = zlib_net.Inflate(br.ReadBytes((int)fileSize), (uint)zBlocks, (uint)cBlockSize, TOC[manifestLocation].OriginalSize);
            }
            else
                outFile = br.ReadBytes((int)TOC[manifestLocation].OriginalSize);

            if (TOC[manifestLocation].OriginalSize != (ulong)outFile.LongLength)
            {
                throw new InvalidDataException(string.Format("Expected size: {0}, Actual size: {1}", TOC[manifestLocation].OriginalSize, outFile.LongLength));
            }

            var output = new UnpackedFile();
            output.FileName = TOC[manifestLocation].FileName;
            output.BinaryFile = outFile;

            var output2 = new UnpackedFile(TOC[manifestLocation].FileName, outFile);
            return output;
        }
        public UnpackedFile DecompressFile(String fileName)
        {
            if (tocList.Contains(fileName)) return DecompressFile(tocList.IndexOf(fileName));
            throw new FileNotFoundException(string.Format("File size: {0} not found.", fileName));
        }

        public PackedFile CompressFile(String fileName, byte[] binaryFile)
        {
            var tmpHeader = new Header(true);
            var tmpEntry = new TOCEntry();
            tmpEntry.FileName = fileName;
            tmpEntry.MD5 = new byte[16];
            tmpEntry.OriginalSize = (ulong)binaryFile.LongLength;
            tmpEntry.StartOffset = 0;
            tmpEntry.BlockListStart = 0;

            try
            {
                byte[] compressedFile = zlib_net.Deflate(binaryFile, tmpHeader.BlockSize);
                return new PackedFile(tmpEntry, compressedFile);
            }
            catch { }

            return new PackedFile();
        }
        static private ulong FortyBitInt(ulong inputData) { return inputData >> 24; }


    }
}
