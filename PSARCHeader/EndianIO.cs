namespace PSARCReader
{
    using System;
    using System.IO;

    public enum EndianType
    {
        BigEndian,
        LittleEndian
    }
    public class EndianReader : BinaryReader
    {
        public EndianType endianstyle;

        public EndianReader(Stream stream, EndianType endianstyle)
            : base(stream)
        {
            this.endianstyle = endianstyle;
        }

        public string ReadAsciiString(int Length)
        {
            return this.ReadAsciiString(Length, this.endianstyle);
        }

        public string ReadAsciiString(int Length, EndianType EndianType)
        {
            string str = "";
            int num = 0;
            for (int i = 0; i < Length; i++)
            {
                char ch = (char)this.ReadByte();
                num++;
                if (ch == '\0')
                {
                    break;
                }
                str = str + ch;
            }
            int num3 = Length - num;
            this.BaseStream.Seek((long)num3, SeekOrigin.Current);
            return str;
        }

        public override double ReadDouble()
        {
            return this.ReadDouble(this.endianstyle);
        }

        public double ReadDouble(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(4);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToDouble(array, 0);
        }

        public override short ReadInt16()
        {
            return this.ReadInt16(this.endianstyle);
        }

        public short ReadInt16(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(2);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToInt16(array, 0);
        }

        public int ReadInt24()
        {
            return this.ReadInt24(this.endianstyle);
        }

        public int ReadInt24(EndianType EndianType)
        {
            byte[] sourceArray = base.ReadBytes(3);
            byte[] destinationArray = new byte[4];
            Array.Copy(sourceArray, 0, destinationArray, 0, 3);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(destinationArray);
            }
            return BitConverter.ToInt32(destinationArray, 0);
        }

        public override int ReadInt32()
        {
            return this.ReadInt32(this.endianstyle);
        }

        public int ReadInt32(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(4);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToInt32(array, 0);
        }

        public override long ReadInt64()
        {
            return this.ReadInt64(this.endianstyle);
        }

        public long ReadInt64(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(8);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToInt64(array, 0);
        }

        public string ReadNullTerminatedString()
        {
            char ch;
            string str = "";
            while ((ch = this.ReadChar()) != '\0')
            {
                if (ch == '\0')
                {
                    return str;
                }
                str = str + ch;
            }
            return str;
        }

        public override float ReadSingle()
        {
            return this.ReadSingle(this.endianstyle);
        }

        public float ReadSingle(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(4);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToSingle(array, 0);
        }

        public string ReadString(int Length)
        {
            return this.ReadAsciiString(Length);
        }

        public override ushort ReadUInt16()
        {
            return this.ReadUInt16(this.endianstyle);
        }

        public ushort ReadUInt16(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(2);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToUInt16(array, 0);
        }

        public override uint ReadUInt32()
        {
            return this.ReadUInt32(this.endianstyle);
        }

        public uint ReadUInt32(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(4);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToUInt32(array, 0);
        }

        public override ulong ReadUInt64()
        {
            return this.ReadUInt64(this.endianstyle);
        }

        public ulong ReadUInt64(EndianType EndianType)
        {
            byte[] array = base.ReadBytes(8);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(array);
            }
            return BitConverter.ToUInt64(array, 0);
        }

        public string ReadUnicodeString(int Length)
        {
            return this.ReadUnicodeString(Length, this.endianstyle);
        }

        public string ReadUnicodeString(int Length, EndianType EndianType)
        {
            string str = "";
            int num = 0;
            for (int i = 0; i < Length; i++)
            {
                char ch = (char)this.ReadUInt16(EndianType);
                num++;
                if (ch == '\0')
                {
                    break;
                }
                str = str + ch;
            }
            int num3 = (Length - num) * 2;
            this.BaseStream.Seek((long)num3, SeekOrigin.Current);
            return str;
        }

        public void SeekTo(int offset)
        {
            this.SeekTo(offset, SeekOrigin.Begin);
        }

        public void SeekTo(long offset)
        {
            this.SeekTo((int)offset, SeekOrigin.Begin);
        }

        public void SeekTo(uint offset)
        {
            this.SeekTo((int)offset, SeekOrigin.Begin);
        }

        public void SeekTo(int offset, SeekOrigin SeekOrigin)
        {
            this.BaseStream.Seek((long)offset, SeekOrigin);
        }
    }
    public class EndianIO
    {
        private EndianReader _in;
        private EndianWriter _out;
        private EndianType endiantype;
        private string filepath;
        private bool isfile;
        private bool isOpen;
        private System.IO.Stream stream;

        public EndianIO(MemoryStream MemoryStream, EndianType EndianStyle)
        {
            this.filepath = "";
            this.endiantype = EndianType.LittleEndian;
            this.endiantype = EndianStyle;
            this.stream = MemoryStream;
            this.isfile = false;
        }

        public EndianIO(System.IO.Stream Stream, EndianType EndianStyle)
        {
            this.filepath = "";
            this.endiantype = EndianType.LittleEndian;
            this.endiantype = EndianStyle;
            this.stream = Stream;
            this.isfile = false;
        }

        public EndianIO(string FilePath, EndianType EndianStyle)
        {
            this.filepath = "";
            this.endiantype = EndianType.LittleEndian;
            this.endiantype = EndianStyle;
            this.filepath = FilePath;
            this.isfile = true;
        }

        public EndianIO(byte[] Buffer, EndianType EndianStyle)
        {
            this.filepath = "";
            this.endiantype = EndianType.LittleEndian;
            this.endiantype = EndianStyle;
            this.stream = new MemoryStream(Buffer);
            this.isfile = false;
        }

        public void Close()
        {
            if (this.isOpen)
            {
                this.stream.Close();
                this._in.Close();
                this._out.Close();
                this.isOpen = false;
            }
        }

        public string trimEnd(string input)
        {
            string trimString = "";
            foreach (char charictor in input)
            {
                if (charictor != ' ')
                {
                    if (charictor == '\0')
                    {
                        break;
                    }
                    else
                    {
                        trimString += charictor.ToString();
                    }
                }
            }
            return trimString;
        }

        public void Open()
        {
            if (!this.isOpen)
            {
                if (this.isfile)
                {
                    this.stream = new FileStream(this.filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                }
                this._in = new EndianReader(this.stream, this.endiantype);
                this._out = new EndianWriter(this.stream, this.endiantype);
                this.isOpen = true;
            }
        }

        public void SeekTo(int offset)
        {
            this.SeekTo(offset, SeekOrigin.Begin);
        }

        public void SeekTo(uint offset)
        {
            this.SeekTo((int)offset, SeekOrigin.Begin);
        }

        public void SeekTo(int offset, SeekOrigin SeekOrigin)
        {
            this.stream.Seek((long)offset, SeekOrigin);
        }

        public bool Closed
        {
            get
            {
                return !this.isOpen;
            }
        }

        public EndianReader In
        {
            get
            {
                return this._in;
            }
        }

        public bool Opened
        {
            get
            {
                return this.isOpen;
            }
        }

        public EndianWriter Out
        {
            get
            {
                return this._out;
            }
        }

        public System.IO.Stream Stream
        {
            get
            {
                return this.stream;
            }
        }
    }
    public class EndianWriter : BinaryWriter
    {
        private EndianType endianstyle;

        public EndianWriter(Stream stream, EndianType endianstyle)
            : base(stream)
        {
            this.endianstyle = endianstyle;
        }

        public void SeekTo(int offset)
        {
            this.SeekTo(offset, SeekOrigin.Begin);
        }

        public void SeekTo(long offset)
        {
            this.SeekTo((int)offset, SeekOrigin.Begin);
        }

        public void SeekTo(uint offset)
        {
            this.SeekTo((int)offset, SeekOrigin.Begin);
        }

        public void SeekTo(int offset, SeekOrigin SeekOrigin)
        {
            this.BaseStream.Seek((long)offset, SeekOrigin);
        }

        public override void Write(double value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(short value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(int value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(long value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(float value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(ushort value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(uint value)
        {
            this.Write(value, this.endianstyle);
        }

        public override void Write(ulong value)
        {
            this.Write(value, this.endianstyle);
        }

        public void Write(double value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(short value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(int value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(long value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(float value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(ushort value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(uint value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void Write(ulong value, EndianType EndianType)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (EndianType == EndianType.BigEndian)
            {
                Array.Reverse(bytes);
            }
            base.Write(bytes);
        }

        public void WriteAsciiString(string String, int Length)
        {
            this.WriteAsciiString(String, Length, this.endianstyle);
        }

        public void WriteAsciiString(string String, int Length, EndianType EndianType)
        {
            int length = String.Length;
            for (int i = 0; i < length; i++)
            {
                if (i > Length)
                {
                    break;
                }
                byte num3 = (byte)String[i];
                this.Write(num3);
            }
            int num4 = Length - length;
            if (num4 > 0)
            {
                this.Write(new byte[num4]);
            }
        }

        public void WriteUnicodeString(string String, int Length)
        {
            this.WriteUnicodeString(String, Length, this.endianstyle);
        }

        public void WriteUnicodeString(string String, int Length, EndianType EndianType)
        {
            int length = String.Length;
            for (int i = 0; i < length; i++)
            {
                if (i > Length)
                {
                    break;
                }
                ushort num3 = String[i];
                this.Write(num3, EndianType);
            }
            int num4 = (Length - length) * 2;
            if (num4 > 0)
            {
                this.Write(new byte[num4]);
            }
        }
    }

}
