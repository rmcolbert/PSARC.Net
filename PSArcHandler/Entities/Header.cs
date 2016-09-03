using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSArcHandler.Entities
{
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
}
