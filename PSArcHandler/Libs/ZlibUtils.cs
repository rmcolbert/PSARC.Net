using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Ionic.Zlib;

namespace PSArcHandler
{
    class ZlibUtils
    {

        public static byte[] Inflate(byte[] compressedStream, uint zBlocks, uint cBlockSize, ulong fileSize)
        {
            var outByte = new List<byte>();
            int data = 0;
            int stopByte = -1;

            long pos = 0;

            // Create an array of decompression streams equal to the number of blocks in the file.
            var zStream = new ZlibStream[zBlocks];
            for (uint i =0; i < zBlocks; i++)
            {
                var zCompressedStream = new MemoryStream(compressedStream);
                zCompressedStream.Seek(pos, SeekOrigin.Begin);
                zStream[i] = new ZlibStream(zCompressedStream, CompressionMode.Decompress);

                while (stopByte != (data = zStream[i].ReadByte()))
                {
                    var _dataByte = (byte)data;
                    outByte.Add(_dataByte);
                }
                pos += zStream[i].TotalIn;

                zStream[i].Close();
            }

            return outByte.ToArray();
        }

        public static byte[] Deflate(byte[] uncompressedStream, ulong cBlockSize)
        {
            ulong zBlocks = (uint)(Math.Ceiling((ulong)uncompressedStream.LongLength / (double)cBlockSize));

            var inStream = new MemoryStream(uncompressedStream);
            var outStream = new MemoryStream();
            byte[] outData;

            var outZStream = new ZlibStream(outStream, CompressionMode.Compress, CompressionLevel.BestCompression);
            try
            {
                CopyStream(inStream, outZStream, cBlockSize);
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

        public static void CopyStream(Stream input, Stream output, ulong cBlockSize)
        {
            var buffer = new byte[cBlockSize];
            int len;
            while ((len = input.Read(buffer, 0, (int)cBlockSize)) > 0)
            {
                output.Write(buffer, 0, len);
            }
            output.Flush();
        }
    }
}
