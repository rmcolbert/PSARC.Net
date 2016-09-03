using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using PSArcHandler.Entities;

namespace PSArcHandler
{
    /// <summary>
    /// This class contains methods to pack and unpack files.
    /// </summary>
    public class PSARC : IDisposable
    {
        #region menbers

        private EndianReader m_erReader;
        private List<String> m_lstTOCList = new List<string>();
        private readonly uint m_intDefaultBlockSize = 0x00010000; // 64KB

        #endregion

        #region Properties

        public Header m_hdrPSHeader = new Header();
        public List<TOCEntry> TOC = new List<TOCEntry>();
        public uint DefaultBlockSize
        {
            get
            {
                return m_intDefaultBlockSize;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
		/// A simple constructor that initializes the object.
		/// </summary>
        public PSARC()
        {
        }

        /// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
        /// <param name="p_strFileName">The file's name</param>
        public PSARC(string p_strFileName)
        {
            LoadStream(new FileStream(p_strFileName, FileMode.Open, FileAccess.Read));
        }

        /// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
        /// <param name="p_ftrArchiveStream">The filestream</param>
        public PSARC(FileStream p_ftrArchiveStream)
        {
            LoadStream(p_ftrArchiveStream);
        }

        ~PSARC()
        {
            if (m_erReader != null) { m_erReader.Close(); m_erReader.Dispose(); m_erReader = null; }
        }

        #endregion

        public void Dispose()
        {
            if (m_erReader != null) { m_erReader.Close(); m_erReader.Dispose(); m_erReader = null; }
        }



        private void LoadStream(FileStream archiveStream)
        {
            if (m_erReader != null) { m_erReader.Close(); m_erReader.Dispose(); m_erReader = null; }
            m_erReader = new EndianReader(archiveStream, EndianType.BigEndian);
            ReadManifest();
        }


        /// <summary>
        /// Set the header 
        /// </summary>
        private void ReadHeader()
        {
            m_erReader.BaseStream.Position = 0;  // Rewind the stream just in case
            m_hdrPSHeader = new Header();
            m_hdrPSHeader.MagicNumber = m_erReader.ReadUInt32();
            m_hdrPSHeader.VersionNumber = m_erReader.ReadUInt32();
            m_hdrPSHeader.CompressionMethod = m_erReader.ReadUInt32();
            m_hdrPSHeader.TocLength = m_erReader.ReadUInt32();
            m_hdrPSHeader.TocEntrySize = m_erReader.ReadUInt32();
            m_hdrPSHeader.TocEntries = m_erReader.ReadUInt32();
            m_hdrPSHeader.BlockSize = m_erReader.ReadUInt32();
            m_hdrPSHeader.ArchiveFlags = m_erReader.ReadUInt32();
        }

        /// <summary>
        /// Read the manifest stored in the file
        /// </summary>
        /// <returns>
        ///     The list of entries in the manifest
        /// </returns>
        public List<TOCEntry> ReadManifest()
        {
            // Always reload the header before reading the manifest
            ReadHeader();

            uint i = 0;
            TOC = new List<TOCEntry>();
            for (i = 0; i < m_hdrPSHeader.TocEntries; i++)
                TOC.Add(new TOCEntry(m_erReader.ReadBytes(16), m_erReader.ReadUInt32(), m_erReader.ReadUInt40(), m_erReader.ReadUInt40()));

            // Extract the Manifest List
            UnpackedFile upfManifest = DecompressFile(0);
            m_lstTOCList = new List<string>(System.Text.Encoding.Default.GetString(upfManifest.BinaryFile).Split('\n'));
            m_lstTOCList.Insert(0, "manifest.txt");  // Insert an entry for the manifest name

            List<TOCEntry> lstTOC = TOC;
            TOC = new List<TOCEntry>();
            for (i = 0; i < lstTOC.Count; i++)
                TOC.Add(new TOCEntry(lstTOC[(int)i], m_lstTOCList[(int)i])); // Create a new TOC that includes the file name from the manifest

            lstTOC = null;
            return TOC;
        }

        /// <summary>
        /// Read the manifest stored in the file
        /// </summary>
        /// <param name="p_strFileName">The file name where the manifest is stored</param>
        /// <returns>
        ///     The list of entries in the manifest
        /// </returns>
        public List<TOCEntry> ReadManifest(string p_strFileName)
        {
            LoadStream(new FileStream(p_strFileName, FileMode.Open, FileAccess.Read));
            return ReadManifest();
        }

        /// <summary>
        /// Decompress a pak file
        /// </summary>
        /// <param name="p_strFileName">Name of the pack file</param>
        /// <returns></returns>
        public UnpackedFile DecompressFile(string p_strFileName)
        {
            if (m_lstTOCList.Contains(p_strFileName)) return DecompressFile(m_lstTOCList.IndexOf(p_strFileName));
            throw new FileNotFoundException(string.Format("File size: {0} not found.", p_strFileName));
        }

        /// <summary>
        /// Decompress a pak file
        /// </summary>
        /// <param name="p_intManifestLocation">The position of the manifest in the pak file</param>
        /// <returns></returns>
        public UnpackedFile DecompressFile(int p_intManifestLocation)
        {
            if (p_intManifestLocation > (m_hdrPSHeader.TocEntries - 1)) return new UnpackedFile();

            byte[] outFile = null;
            m_erReader.BaseStream.Position = (long)TOC[p_intManifestLocation].StartOffset; // From manifest

            UInt16 isZipped = m_erReader.ReadUInt16();
            m_erReader.BaseStream.Position -= 2;    // Rewind 2 bytes

            ulong cBlockSize = m_hdrPSHeader.BlockSize;

            ulong zBlocks = (uint)(Math.Ceiling(TOC[p_intManifestLocation].OriginalSize / (double)cBlockSize));

            if (isZipped == 0x78da || isZipped == 0x7801) // Stream is compressed
            {
                ulong fileSize = zBlocks * cBlockSize;  // Only pass a part of the whole archive stream to be inflated.
                outFile = ZlibUtils.Inflate(m_erReader.ReadBytes((int)fileSize), (uint)zBlocks, (uint)cBlockSize, TOC[p_intManifestLocation].OriginalSize);
            }
            else
                outFile = m_erReader.ReadBytes((int)TOC[p_intManifestLocation].OriginalSize);

            if (TOC[p_intManifestLocation].OriginalSize != (ulong)outFile.LongLength)
            {
                throw new InvalidDataException(string.Format("Expected size: {0}, Actual size: {1}", TOC[p_intManifestLocation].OriginalSize, outFile.LongLength));
            }

            var output = new UnpackedFile();
            output.FileName = TOC[p_intManifestLocation].FileName;
            output.BinaryFile = outFile;

            var output2 = new UnpackedFile(TOC[p_intManifestLocation].FileName, outFile);
            return output;
        }

        

        public PackedFile CompressFile(String fileName, byte[] binaryFile)
        {
            var tmpHeader = new Header(true);
            var tmpEntry = new TOCEntry();
            tmpEntry.FileName = fileName.Replace('\\','/');
            tmpEntry.MD5 = new byte[16];
            tmpEntry.OriginalSize = (ulong)binaryFile.LongLength;
            tmpEntry.StartOffset = 0;
            tmpEntry.BlockListStart = (uint)(Math.Ceiling(tmpEntry.OriginalSize / (double)tmpHeader.BlockSize));

            try
            {
                byte[] compressedFile = ZlibUtils.Deflate(binaryFile, tmpHeader.BlockSize);
                return new PackedFile(tmpEntry, compressedFile);
            }
            catch { }

            return new PackedFile();
        }
        static private ulong FortyBitInt(ulong inputData) { return inputData >> 24; }


    }
}
