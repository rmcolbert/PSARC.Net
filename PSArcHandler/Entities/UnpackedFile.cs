using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSArcHandler.Entities
{
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
}
