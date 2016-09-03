using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSArcHandler;
using PSArcHandler.Entities;

namespace PSARCHeader
{
    class Program
    {
        //static readonly string fileName = @".\TestFiles\NMSARC.B9700502.pak"; // 920MB PAK
        static readonly string fileName = @".\TestFiles\NMSARC.8A8FE611.pak"; // 10MB PAK w/ DEDUPED FILES
        static readonly string m_strTestDirectories = @".\TestFiles"; // 10MB PAK w/ DEDUPED FILES

        private PSARC pSarc;

        static void Main(string[] args)
        {
            var program = new Program();
            program.Run(args);
        }
        void Run(string[] args)
        {
            UnpackMultipleFiles(m_strTestDirectories);

            //Test reading archive from disk directly via ReadManifest
            pSarc = new PSARC();
            pSarc.ReadManifest(fileName);
            if (pSarc.TOC.Count != pSarc.m_hdrPSHeader.TocEntries) throw new Exception("TOC Count does not meet expected value");

            // Test reading archive from disk directly via constructor
            pSarc = new PSARC(fileName);
            pSarc.ReadManifest();
            if (pSarc.TOC.Count != pSarc.m_hdrPSHeader.TocEntries) throw new Exception("TOC Count does not meet expected value");

            // Test reading archive from FileStream
            pSarc = new PSARC(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            pSarc.ReadManifest();
            if (pSarc.TOC.Count != pSarc.m_hdrPSHeader.TocEntries) throw new Exception("TOC Count does not meet expected value");

            int index = 0;
            var manifest = new List<string>();
            foreach (TOCEntry entry in pSarc.TOC)
            {
                string msg = string.Format("Index: {0}, Name: {1}, \toriginalSize: {2}, \tstartOffset: {3}, \tblockListStart: {4}",
                    index,
                    entry.FileName.Split('\\', '/').Last(),
                    entry.OriginalSize,
                    entry.StartOffset,
                    entry.BlockListStart);
                manifest.Add(msg);
                Console.WriteLine(msg);

                index++;
            }
            File.WriteAllLines(@".\TestFiles\manifestdetails.txt", manifest);
            Console.Write("Press any key to continue ...");
            Console.ReadKey();

            TestDecompression();
            TestCompression();
        }

        /// <summary>
        /// Unpack all the .pak files in the directory
        /// </summary>
        /// <param name="p_strDirectoryPath">The directory's path</param>
        private void UnpackMultipleFiles(string p_strDirectoryPath)
        {
            string[] lstFiles = Directory.GetFiles(p_strDirectoryPath, "*.pak");
            Parallel.ForEach<string>(lstFiles, s => DecompressPAKFile(s));

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }

        /// <summary>
        /// Decompress a .Pak file
        /// </summary>
        /// <param name="p_strFileName">The file's name</param>
        private void DecompressPAKFile(string p_strFileName)
        {
            //Throw exection if the file isn't a .pak
            if(!p_strFileName.Contains(".pak"))
                throw new Exception(string.Format("The file {0} isn't a .pak file", p_strFileName));

            using (PSARC psaDecompresser = new PSARC(p_strFileName))
            {
                psaDecompresser.ReadManifest();

                if (psaDecompresser.TOC.Count != psaDecompresser.m_hdrPSHeader.TocEntries)
                    throw new Exception("TOC Count does not meet expected value");

                foreach (TOCEntry tocEntry in psaDecompresser.TOC)
                {
                    //Skip the manifest files
                    if (tocEntry.FileName.Contains("manifest"))
                        continue;
                    WriteFile(psaDecompresser.DecompressFile(tocEntry.FileName));
                }
            }
            Console.WriteLine(string.Format(" Files: {0} is successfully uncompress", p_strFileName));
        }

        void WriteFile(UnpackedFile arcFile)
        {
            string outputFileName = @".\TestFiles\" + arcFile.FileName.Replace('/', '\\');
            string outputFolder = outputFileName.Substring(0, outputFileName.LastIndexOf('\\'));

            Directory.CreateDirectory(outputFolder);
            File.WriteAllBytes(outputFileName, arcFile.BinaryFile);
        }

        void TestDecompression()
        {
            // Test expanding files by index
            Console.WriteLine("Testing file expansion by TOC index");
            for (int i = 0; i < pSarc.TOC.Count - 1; i++)
                WriteFile(pSarc.DecompressFile(i));

            Console.Write("Press any key to continue ...");
            Console.ReadKey();

            // Test expanding files by name
            Console.WriteLine("Testing file expansion by file name");
            for (int i = 0; i < pSarc.TOC.Count - 1; i++)
                WriteFile(pSarc.DecompressFile(pSarc.TOC[i].FileName));

            Console.Write("Press any key to continue ...");
            Console.ReadKey();

            // Test expanding by foreach
            Console.WriteLine("Testing file expansion using foreach on TOC");
            foreach (TOCEntry tocEntry in pSarc.TOC)
                WriteFile(pSarc.DecompressFile(tocEntry.FileName));

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }

        void TestCompression()
        {
            // Test creating ZLib compressed blobs
            // var soureDir = new DirectoryInfo(@".\TestFiles");
            GetFileList(@".\TestFiles");


            Console.WriteLine("Testing individual zlib file compression");
            foreach (string file in fileList)
            {
                if (file.Contains(".pak")) continue;
                PackedFile packed = pSarc.CompressFile(file, File.ReadAllBytes(file));

                Console.WriteLine("Packed file: {0}, Blocks: {1}, Original Size: {2}, Compressed Size: {3}",
                    packed.TocEntry.FileName, packed.TocEntry.BlockListStart, packed.TocEntry.OriginalSize, packed.CompressedFile.LongLength);
            }

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }

        readonly List<string> fileList = new List<string>();
        void GetFileList(string path)
        {
            foreach (string d in Directory.GetDirectories(path))
            {
                foreach (string f in Directory.GetFiles(d)) fileList.Add(f);
                GetFileList(d);
            }
        }
    }
}
