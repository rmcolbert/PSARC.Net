using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Ionic.Zlib;

namespace PSArcHandler
{
    class zlib_net
    {

        public static byte[] Inflate(byte[] CompressedStream, uint zBlocks, uint cBlockSize, ulong fileSize)
        {
            List<byte> outByte = new List<byte>();
            int data = 0;
            int stopByte = -1;

            long pos = 0;

            // Create an array of decompression streams equal to the number of blocks in the file.
            ZlibStream[] zStream = new ZlibStream[zBlocks];
            for (uint i =0; i < zBlocks; i++)
            {
                MemoryStream zCompressedStream = new MemoryStream(CompressedStream);
                zCompressedStream.Seek(pos, SeekOrigin.Begin);
                zStream[i] = new ZlibStream(zCompressedStream, CompressionMode.Decompress);

                while (stopByte != (data = zStream[i].ReadByte()))
                {
                    byte _dataByte = (byte)data;
                    outByte.Add(_dataByte);
                }
                pos += zStream[i].TotalIn;

                zStream[i].Close();
            }

            return outByte.ToArray();
        }

        public static byte[] Deflate(byte[] UncompressedStream)
        {
            MemoryStream inStream = new MemoryStream(UncompressedStream);
            MemoryStream outStream = new MemoryStream();
            byte[] outData;

            ZlibStream outZStream = new ZlibStream(outStream, CompressionMode.Compress, CompressionLevel.Default);
            try
            {
                CopyStream(inStream, outZStream);
                outData = new byte[outStream.Length];
                outStream.Read(outData, 0, (int)outStream.Length);
            }
            finally
            {
                outZStream.Close();
                outStream.Close();
                inStream.Close();
            }
            return outData;
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[2000];
            int len;
            while ((len = input.Read(buffer, 0, 2000)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
