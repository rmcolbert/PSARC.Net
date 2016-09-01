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
                PSARC.UnpackedFile arcFile = pSARC.decompressFile(pSARC.TOC[i].fileName);
                FileStream output = new FileStream(@".\TestFiles\" + arcFile.fileName.Split('\\', '/').Last(), FileMode.OpenOrCreate);
                Console.WriteLine("Saving {0} to {1}", arcFile.fileName, output.Name);
                output.Write(arcFile.binaryFile, 0, arcFile.binaryFile.Length);
                output.Close();
            }

            DirectoryInfo soureDir = new DirectoryInfo(@".\TestFiles");

            foreach (FileInfo file in soureDir.GetFiles())
            {
                FileStream input = file.OpenRead();
                byte[] binaryInput = new byte[input.Length];
                input.Read(binaryInput, 0, (int)input.Length); input.Close();
                PSARC.PackedFile packed = pSARC.compressFile(file.Name, binaryInput);

                Console.WriteLine("Packed file: {0}, Blocks: {1}, Original Size: {2}, Compressed Size: {3}",
                    packed.TOCEntry.fileName, packed.TOCEntry.blockListStart, packed.TOCEntry.originalSize, packed.compressedFile.LongLength);
            }


        }
    }
}
