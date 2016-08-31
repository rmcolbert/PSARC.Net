using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PSArcHandler
{
    class zlib_net
    {
        public static byte[] Inflate(byte[] CompressedStream)
        {
            List<byte> outByte = new List<byte>();
            int data = 0;
            int stopByte = -1;
            zlib.ZInputStream inZStream = new zlib.ZInputStream(new MemoryStream(CompressedStream));
            while (stopByte != (data = inZStream.Read()))
            {
                byte _dataByte = (byte)data;
                outByte.Add(_dataByte);
            }
            inZStream.Close();
            return outByte.ToArray();
        }

        public static byte[] Deflate(byte[] UncompressedStream)
        {
            MemoryStream inStream = new MemoryStream(UncompressedStream);
            MemoryStream outStream = new MemoryStream();
            byte[] outData;
            zlib.ZOutputStream outZStream = new zlib.ZOutputStream(outStream, zlib.zlibConst.Z_DEFAULT_COMPRESSION);
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
