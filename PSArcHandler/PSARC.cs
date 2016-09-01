using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace PSArcHandler
{
    public class PSARC
    {
        private string archiveFileName = "";
        private EndianReader br;
        private readonly uint defaultBlockSize = 0x00010000; // 64KB

        private struct Header
        {
            public uint magicNumber;        // 0x50534152 'PSAR'
            public uint versionNumber;
            public uint compressionMethod;  // zlib or lzma
            public uint tocLength;          // Includes Header
            public uint tocEntrySize;       // Size of a single entry in the TOC, currently 30. This allows the size to be expanded in the future while maintaining backward compat.
            public uint tocEntries;         // Total number of entries including the manifest
            public uint blockSize;          // The size of each block decompressed.
            public uint archiveFlags;

            public Header(bool isDefault) : this(){
                if (isDefault)
                {
                    magicNumber = 0x50534152;
                    versionNumber = 0x00010004;
                    compressionMethod = 0x7a6c6962;
                    blockSize = 0x00010000; // 64KB
                    archiveFlags = 0x0;
                }
            }
        }
        private Header psHeader = new Header();

        public struct TOCEntry
        {
            public byte[] MD5;              // hash of FILENAME.
            public uint blockListStart;     // Not used in this code for decompression but needs to be calculated on compression
            public ulong originalSize;      // UInt40: Read UInt64 and then back the pointer up by 3!!
            public ulong startOffset;       // UInt40: Read UInt64 and then back the pointer up by 3!!
            public string fileName;

            public TOCEntry(byte[] MD5, uint blockListStart, ulong originalSize) : this()
            {
                this.MD5 = MD5;
                this.blockListStart = blockListStart;
                this.originalSize = originalSize;
            }
        }
        public List<TOCEntry> TOC = new List<TOCEntry>();
        private List<String> TOCList = new List<string>();

        public struct UnpackedFile 
        {
            public String fileName;
            public byte[] binaryFile;

            public UnpackedFile(string fileName, byte[] outFile) : this()
            {
                this.fileName = fileName;
                this.binaryFile = outFile;
            }
        }

        public struct PackedFile
        {
            public TOCEntry TOCEntry;
            public byte[] compressedFile;

            public PackedFile(TOCEntry TOCEntry, byte[] compressedFile) : this()
            {
                this.TOCEntry = TOCEntry;
                this.compressedFile = compressedFile;
            }
        }
        public PSARC()
        {
        }

        public PSARC(String fileName)
        {
            archiveFileName = fileName;
            br = new EndianReader(new FileStream(archiveFileName, FileMode.Open, FileAccess.Read), EndianType.BigEndian);
            readHeader();
        }

        ~PSARC()
        {
            if (br != null) { br.Close(); br.Dispose(); br = null; }
        }

        private void readHeader()
        {
            br.BaseStream.Position = 0;  // Rewind the stream just in case
            psHeader = new Header();
            psHeader.magicNumber = br.ReadUInt32();
            psHeader.versionNumber = br.ReadUInt32();
            psHeader.compressionMethod = br.ReadUInt32();
            psHeader.tocLength = br.ReadUInt32();
            psHeader.tocEntrySize = br.ReadUInt32();
            psHeader.tocEntries = br.ReadUInt32();
            psHeader.blockSize = br.ReadUInt32();
            psHeader.archiveFlags = br.ReadUInt32();
        }

        public List<TOCEntry> readManifest(string fileName)
        {
            // Check to see if this is the arc file already open.
            if (fileName.ToLowerInvariant() != archiveFileName.ToLowerInvariant())
            {
                archiveFileName = fileName;
                // Make sure we close any open arc file and release any used memory before opening a new arc file
                if (br != null) { br.Close(); br.Dispose(); br = null; }
                br = new EndianReader(new FileStream(archiveFileName, FileMode.Open, FileAccess.Read), EndianType.BigEndian);
                readHeader();
            }
            return readManifest();
        }

        public List<TOCEntry> readManifest()
        {
            uint i = 0;
            TOC = new List<TOCEntry>();
            for (i = 0; i < psHeader.tocEntries; i++)
            {
                TOCEntry tmp = new TOCEntry();
                tmp.MD5 = br.ReadBytes(16);
                tmp.blockListStart = br.ReadUInt32();
                tmp.originalSize = FortyBitInt(br.ReadUInt64());
                br.BaseStream.Position -= 3;
                tmp.startOffset = FortyBitInt(br.ReadUInt64());
                br.BaseStream.Position -= 3;
                TOC.Add(tmp);
            }

            // Extract the Manifest List
            UnpackedFile manifest = decompressFile(0);
            TOCList = new List<string>(System.Text.Encoding.Default.GetString(manifest.binaryFile).Split('\n'));

            List<TOCEntry> TOCtmp = TOC;
            TOC = new List<TOCEntry>();
            for (i = 0; i < TOCtmp.Count; i++)
            {
                TOCEntry tmp = TOCtmp[(int)i];
                if (i>0) tmp.fileName = TOCList[(int)i-1]; // The manifest doesn't have a file name, so skip the first TOC entry.
                TOC.Add(tmp);
            }
            TOCtmp = null;
            return TOC;
        }

         public UnpackedFile decompressFile(int manifestLocation)
        {
            byte[] outFile = null;
            br.BaseStream.Position = (long)TOC[manifestLocation].startOffset; // From manifest

            UInt16 isZipped = br.ReadUInt16();
            br.BaseStream.Position -= 2;    // Rewind 2 bytes

            ulong cBlockSize = psHeader.blockSize;

            // Calculate the number of blocks the uncompressed file would consume (this is more data than needed for decompression)
            //ulong zBlocks = ((TOC[manifestLocation].originalSize - (TOC[manifestLocation].originalSize % cBlockSize)) / cBlockSize)
            //               + (TOC[manifestLocation].originalSize % cBlockSize) == 0 ? 0u : 1u;

            ulong zBlocks = ((TOC[manifestLocation].originalSize - (TOC[manifestLocation].originalSize % cBlockSize)) / cBlockSize);
            if (TOC[manifestLocation].originalSize % cBlockSize > 0) zBlocks++;


            if (isZipped == 0x78da || isZipped == 0x7801) // Stream is compressed
            {
                ulong fileSize = zBlocks * cBlockSize;  // Only pass a part of the whole archive stream to be inflated.
                outFile = zlib_net.Inflate(br.ReadBytes((int)fileSize), (uint)zBlocks, (uint)cBlockSize, TOC[manifestLocation].originalSize);
            } 
            else
                outFile = br.ReadBytes((int)TOC[manifestLocation].originalSize);

            if (TOC[manifestLocation].originalSize != (ulong)outFile.LongLength)
            {
                throw new InvalidDataException(String.Format("Expected size: {0}, Actual size: {1}", TOC[manifestLocation].originalSize, outFile.LongLength));
            }

            UnpackedFile output = new UnpackedFile();
            output.fileName = TOC[manifestLocation].fileName;
            output.binaryFile = outFile;

            UnpackedFile output2 = new UnpackedFile(TOC[manifestLocation].fileName, outFile);
            return output;
        }
        public UnpackedFile decompressFile(String fileName)
        {
            if (TOCList.Contains(fileName))     return decompressFile(1 + TOCList.IndexOf(fileName));
            throw new System.IO.FileNotFoundException(String.Format("File size: {0} not found in {1}", fileName, archiveFileName));
        }

        public PackedFile compressFile(String fileName, byte[] binaryFile)
        {
            Header tmpHeader = new Header(true);
            TOCEntry tmpEntry = new TOCEntry();
            tmpEntry.fileName = fileName;
            tmpEntry.MD5 = new byte[16];
            tmpEntry.originalSize = (ulong)binaryFile.LongLength;
            tmpEntry.startOffset = 0;
            tmpEntry.blockListStart = 0;

            try
            {
                byte[] compressedFile = zlib_net.Deflate(binaryFile, tmpHeader.blockSize);
                return new PackedFile(tmpEntry, compressedFile);
            } catch { }

            return new PackedFile();
        }
        static private ulong FortyBitInt(ulong InputData) { return InputData >> 24; }
    }
}
