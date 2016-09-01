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

        private struct Header
        {
            public uint MagicNumber; // 0x50534152 'PSAR'
            public uint VersionNumber;
            public uint CompressionMethod; // zlib or lzma
            public uint TotalTOCSize; // Includes Header
            public uint TOCEntrySize; // Size of a single entry in the TOC, currently 30. This allows the size to be expanded in the future while maintaining backward compat.
            public uint numFiles;
            public uint blockSize; // The size of each block decompressed.
            public uint archiveFlags;
        }
        private Header psHeader = new Header();

        public struct TOCEntry
        {
            public byte[] MD5; // len16, all 0's hash is used for the manifest file.
            public uint blockListStart;
            public ulong originalSize;
            public ulong startOffset; // BACK THE READING POINTER UP BY 3!!
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
            psHeader.MagicNumber = br.ReadUInt32();
            psHeader.VersionNumber = br.ReadUInt32();
            psHeader.CompressionMethod = br.ReadUInt32();
            psHeader.TotalTOCSize = br.ReadUInt32();
            psHeader.TOCEntrySize = br.ReadUInt32();
            psHeader.numFiles = br.ReadUInt32();
            psHeader.blockSize = br.ReadUInt32();
            psHeader.archiveFlags = br.ReadUInt32();
        }

        public List<TOCEntry> readManifest()
        {
            uint i = 0;
            TOC = new List<TOCEntry>();
            for (i = 0; i < psHeader.numFiles; i++)
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

            uint zBlocks = TOC[1].blockListStart - TOC[0].blockListStart;
            uint cBlockSize = psHeader.blockSize;

            // Extract the Manifest File
            br.BaseStream.Position = (long)TOC[0].startOffset; // Manifest File
            byte[] DecompressedStream = zlib_net.Inflate(br.ReadBytes((int)(TOC[1].startOffset - TOC[0].startOffset)), zBlocks, cBlockSize, TOC[0].originalSize);

            TOCList = new List<string>(System.Text.Encoding.Default.GetString(DecompressedStream).Split('\n'));

            List<TOCEntry> TOCtmp = TOC;
            TOC = new List<TOCEntry>();
            for (i = 0; i < TOCtmp.Count; i++)
            {
                TOCEntry tmp = TOCtmp[(int)i];
                if (i>0) tmp.fileName = TOCList[(int)i-1]; // The manifest doesn't have a file name, so skip the first TOC entry.
                TOC.Add(tmp);
            }

            //TOC = TOC.OrderBy(sel => sel.blockListStart).ToList();
            return TOC;
        }

        public List<TOCEntry> readManifest(string fileName)
        {
            // Check to see if this is the arc file already open.
            if (fileName.ToLowerInvariant() != archiveFileName.ToLowerInvariant()) {
                archiveFileName = fileName;
                // Make sure we close any open arc file and release any used memory before opening a new arc file
                if (br != null) { br.Close(); br.Dispose(); br = null; }
                br = new EndianReader(new FileStream(archiveFileName, FileMode.Open, FileAccess.Read), EndianType.BigEndian);
                readHeader();
            }
            return readManifest();
        }

        public UnpackedFile readFile(int manifestLocation)
        {
            byte[] outFile = null;
            br.BaseStream.Position = (long)TOC[manifestLocation].startOffset; // Manifest File
            UInt16 isGzipped = br.ReadUInt16();
            br.BaseStream.Position -= 2;    // Rewind 2 bytes

            uint zBlocks = TOC[manifestLocation + 1].blockListStart - TOC[manifestLocation].blockListStart;
            uint cBlockSize = psHeader.blockSize;

            ulong fileSize = TOC[manifestLocation + 1].startOffset - TOC[manifestLocation].startOffset;

            if (isGzipped == 0x78da || isGzipped == 0x7801)
                outFile = zlib_net.Inflate(br.ReadBytes((int)fileSize), zBlocks, cBlockSize, TOC[manifestLocation].originalSize);
            else
                outFile = br.ReadBytes((int)fileSize);

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
        public UnpackedFile readFile(String fileName)
        {
            if (TOCList.Contains(fileName))     return readFile(1 + TOCList.IndexOf(fileName));
            
            throw new System.IO.FileNotFoundException(String.Format("File size: {0} not found in {1}", fileName, archiveFileName));
        }

        static private ulong FortyBitInt(ulong InputData)
        {
            return InputData >> 24;
        }
    }
}
