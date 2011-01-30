﻿using System;

namespace BTDB
{
    public abstract class AbstractBufferedWriter
    {
        protected AbstractBufferedWriter()
        {
            Buf = null;
            Pos = 0;
            End = 0;
        }
        protected byte[] Buf;
        protected int Pos;
        protected int End;

        public abstract void FlushBuffer();

        public void WriteVInt32(int value)
        {
            if (value < 0) WriteVUInt64(((uint)-(value + 1)) * 2 + 1);
            else WriteVUInt64((uint)value * 2);
        }

        public void WriteVUInt32(uint value)
        {
            WriteVUInt64(value);
        }

        public void WriteVInt64(long value)
        {
            if (value < 0) WriteVUInt64(((ulong)-(value + 1)) * 2 + 1);
            else WriteVUInt64((ulong)value * 2);
        }

        public void WriteVUInt64(ulong value)
        {
            var l = PackUnpack.LengthVUInt(value);
            if (Pos + l > End)
            {
                FlushBuffer();
                if (Pos + l > End)
                {
                    var b = new byte[l];
                    int i = 0;
                    PackUnpack.PackVUInt(b, ref i, value);
                    WriteBlock(b);
                    return;
                }
            }
            PackUnpack.PackVUInt(Buf, ref Pos, value);
        }

        public void WriteInt64(long value)
        {
            if (Pos + 8 > End)
            {
                FlushBuffer();
                if (Pos + 8 > End)
                {
                    var b = new byte[8];
                    PackUnpack.PackInt64BE(b, 0, value);
                    WriteBlock(b);
                    return;
                }
            }
            PackUnpack.PackInt64BE(Buf, Pos, value);
            Pos += 8;
        }

        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.ToBinary());
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteVUInt64(0);
                return;
            }
            var l = value.Length;
            WriteVUInt64((ulong)l + 1);
            int i = 0;
            while (i < l)
            {
                var c = value[i];
                if (char.IsHighSurrogate(c) && i + 1 < l)
                {
                    var c2 = value[i + 1];
                    if (char.IsLowSurrogate(c2))
                    {
                        WriteVUInt32((uint)((((c - 0xD800) * 0x400) + (c2 - 0xDC00)) + 0x10000));
                        i += 2;
                        continue;
                    }
                }
                WriteVUInt32(c);
                i++;
            }
        }

        public void WriteBlock(byte[] data, int offset, int length)
        {
            while (length > 0)
            {
                if (Pos >= End) FlushBuffer();
                var l = End - Pos;
                if (length < l) l = length;
                Array.Copy(data, offset, Buf, Pos, l);
                offset += l;
                length -= l;
                Pos += l;
            }
        }

        public void WriteBlock(byte[] data)
        {
            WriteBlock(data, 0, data.Length);
        }

        public void WriteGuid(Guid value)
        {
            WriteBlock(value.ToByteArray());
        }
    }
}
