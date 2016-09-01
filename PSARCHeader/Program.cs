using System;
using System.IO;
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
            PSARC pSARC = new PSARC();

            string fileName     = @".\TestFiles\NMSARC.8A8FE611.pak";
            pSARC.readManifest(fileName);

            int index = 0;
            List<string> manifest = new List<string>();
            foreach (PSARC.TOCEntry entry in pSARC.TOC)
            {
                manifest.Add(
                string.Format("Index: {0}, Name: {1}, \toriginalSize: {2}, \tstartOffset: {3}, \tblockListStart: {4}",
                    index,
                    String.IsNullOrEmpty(entry.fileName) ? "manifest" : entry.fileName.Split('\\', '/').Last(),
                    entry.originalSize,
                    entry.startOffset,
                    entry.blockListStart)
                );

                index++;
            }
            File.WriteAllLines(@".\TestFiles\manifest.txt", manifest);


            for (int i = 1; i < pSARC.TOC.Count; i++)
            {
                PSARC.UnpackedFile arcFile = pSARC.readFile(pSARC.TOC[i].fileName);
                FileStream output = new FileStream(@".\TestFiles\" + arcFile.fileName.Split('\\', '/').Last(), FileMode.OpenOrCreate);
                output.Write(arcFile.binaryFile, 0, arcFile.binaryFile.Length);
                output.Close();
            }
        }
    }
}
