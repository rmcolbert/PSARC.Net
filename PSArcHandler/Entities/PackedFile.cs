using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSArcHandler.Entities
{
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
}
