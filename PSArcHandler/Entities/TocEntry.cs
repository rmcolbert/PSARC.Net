using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSArcHandler.Entities
{
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
}
