using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSArcHandler;

namespace PSARCHeader
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileName     = @"C:\Source\Repos\psarc-tool\bin\NMSARC.8A8FE611.pak";
            List<String> manifest = PSARC.readManifest(fileName);
        }
    }
}
