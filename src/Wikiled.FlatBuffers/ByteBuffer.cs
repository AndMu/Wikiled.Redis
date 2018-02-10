#define UNSAFE_BYTEBUFFER // uncomment this line to use faster ByteBuffer

using System;

namespace Wikiled.FlatBuffers
{
    /// <summary>
    ///     Class to mimic Java's ByteBuffer which is used heavily in Flatbuffers.
    ///     If your execution environment allows unsafe code, you should enable
    ///     unsafe code in your project and #define UNSAFE_BYTEBUFFER to use a
    ///     MUCH faster version of ByteBuffer.
    /// </summary>
    public class ByteBuffer
    {
        public int Length
        {
            get
            {
                return Data.Length;
            }
        }

        public byte[] Data { get; }

        public ByteBuffer(byte[] buffer)
        {
            Data = buffer;
            Position = 0;
        }

        public int Position { get; private set; }

        // Pre-allocated helper arrays for convertion.
        private float[] floathelper = { 0.0f };

        private int[] inthelper = { 0 };

        private double[] doublehelper = { 0.0 };

        private ulong[] ulonghelper = { 0UL };

        // Helper functions for the unsafe version.
        public static ushort ReverseBytes(ushort input)
        {
            return (ushort)(((input & 0x00FFU) << 8) | ((input & 0xFF00U) >> 8));
        }

        public static uint ReverseBytes(uint input)
        {
            return ((input & 0x000000FFU) << 24) | ((input & 0x0000FF00U) << 8) | ((input & 0x00FF0000U) >> 8) | ((input & 0xFF000000U) >> 24);
        }

        public static ulong ReverseBytes(ulong input)
        {
            return ((input & 0x00000000000000FFUL) << 56) |
                   ((input & 0x000000000000FF00UL) << 40) |
                   ((input & 0x0000000000FF0000UL) << 24) |
                   ((input & 0x00000000FF000000UL) << 8) |
                   ((input & 0x000000FF00000000UL) >> 8) |
                   ((input & 0x0000FF0000000000UL) >> 24) |
                   ((input & 0x00FF000000000000UL) >> 40) |
                   ((input & 0xFF00000000000000UL) >> 56);
        }

#if !UNSAFE_BYTEBUFFER

// Helper functions for the safe (but slower) version.
        protected void WriteLittleEndian(int offset, int count, ulong data)
        {
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < count; i++)
                {
                    _buffer[offset + i] = (byte)(data >> i * 8);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    _buffer[offset + count - 1 - i] = (byte)(data >> i * 8);
                }
            }
            _pos = offset;
        }

        protected ulong ReadLittleEndian(int offset, int count)
        {
            AssertOffsetAndLength(offset, count);
            ulong r = 0;
            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < count; i++)
                {
                  r |= (ulong)_buffer[offset + i] << i * 8;
                }
            }
            else
            {
              for (int i = 0; i < count; i++)
              {
                r |= (ulong)_buffer[offset + count - 1 - i] << i * 8;
              }
            }
            return r;
        }
#endif // !UNSAFE_BYTEBUFFER

        private void AssertOffsetAndLength(int offset, int length)
        {
            if (offset < 0 ||
                offset >= Data.Length ||
                offset + length > Data.Length)
                throw new ArgumentOutOfRangeException();
        }

        public void PutSbyte(int offset, sbyte value)
        {
            AssertOffsetAndLength(offset, sizeof(sbyte));
            Data[offset] = (byte)value;
            Position = offset;
        }

        public void PutByte(int offset, byte value)
        {
            AssertOffsetAndLength(offset, sizeof(byte));
            Data[offset] = value;
            Position = offset;
        }

#if UNSAFE_BYTEBUFFER

        // Unsafe but more efficient versions of Put*.
        public void PutShort(int offset, short value)
        {
            PutUshort(offset, (ushort)value);
        }

        public unsafe void PutUshort(int offset, ushort value)
        {
            AssertOffsetAndLength(offset, sizeof(ushort));
            fixed (byte* ptr = Data)
            {
                *(ushort*)(ptr + offset) = BitConverter.IsLittleEndian ? value : ReverseBytes(value);
            }

            Position = offset;
        }

        public void PutInt(int offset, int value)
        {
            PutUint(offset, (uint)value);
        }

        public unsafe void PutUint(int offset, uint value)
        {
            AssertOffsetAndLength(offset, sizeof(uint));
            fixed (byte* ptr = Data)
            {
                *(uint*)(ptr + offset) = BitConverter.IsLittleEndian ? value : ReverseBytes(value);
            }

            Position = offset;
        }

        public void PutLong(int offset, long value)
        {
            PutUlong(offset, (ulong)value);
        }

        public unsafe void PutUlong(int offset, ulong value)
        {
            AssertOffsetAndLength(offset, sizeof(ulong));

            fixed (byte* ptr = Data)
            {
                *(ulong*)(ptr + offset) = BitConverter.IsLittleEndian ? value : ReverseBytes(value);
            }

            Position = offset;
        }

        public unsafe void PutFloat(int offset, float value)
        {
            AssertOffsetAndLength(offset, sizeof(float));
            fixed (byte* ptr = Data)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(float*)(ptr + offset) = value;
                }
                else
                {
                    *(uint*)(ptr + offset) = ReverseBytes(*(uint*)&value);
                }
            }

            Position = offset;
        }

        public unsafe void PutDouble(int offset, double value)
        {
            AssertOffsetAndLength(offset, sizeof(double));
            fixed (byte* ptr = Data)
            {
                if (BitConverter.IsLittleEndian)
                {
                    *(double*)(ptr + offset) = value;
                }
                else
                {
                    *(ulong*)(ptr + offset) = ReverseBytes(*(ulong*)(ptr + offset));
                }
            }

            Position = offset;
        }

#else // !UNSAFE_BYTEBUFFER

// Slower versions of Put* for when unsafe code is not allowed.
        public void PutShort(int offset, short value)
        {
            AssertOffsetAndLength(offset, sizeof(short));
            WriteLittleEndian(offset, sizeof(short), (ulong)value);
        }

        public void PutUshort(int offset, ushort value)
        {
            AssertOffsetAndLength(offset, sizeof(ushort));
            WriteLittleEndian(offset, sizeof(ushort), (ulong)value);
        }

        public void PutInt(int offset, int value)
        {
            AssertOffsetAndLength(offset, sizeof(int));
            WriteLittleEndian(offset, sizeof(int), (ulong)value);
        }

        public void PutUint(int offset, uint value)
        {
            AssertOffsetAndLength(offset, sizeof(uint));
            WriteLittleEndian(offset, sizeof(uint), (ulong)value);
        }

        public void PutLong(int offset, long value)
        {
            AssertOffsetAndLength(offset, sizeof(long));
            WriteLittleEndian(offset, sizeof(long), (ulong)value);
        }

        public void PutUlong(int offset, ulong value)
        {
            AssertOffsetAndLength(offset, sizeof(ulong));
            WriteLittleEndian(offset, sizeof(ulong), value);
        }

        public void PutFloat(int offset, float value)
        {
            AssertOffsetAndLength(offset, sizeof(float));
            floathelper[0] = value;
            Buffer.BlockCopy(floathelper, 0, inthelper, 0, sizeof(float));
            WriteLittleEndian(offset, sizeof(float), (ulong)inthelper[0]);
        }

        public void PutDouble(int offset, double value)
        {
            AssertOffsetAndLength(offset, sizeof(double));
            doublehelper[0] = value;
            Buffer.BlockCopy(doublehelper, 0, ulonghelper, 0, sizeof(double));
            WriteLittleEndian(offset, sizeof(double), ulonghelper[0]);
        }

#endif // UNSAFE_BYTEBUFFER

        public sbyte GetSbyte(int index)
        {
            AssertOffsetAndLength(index, sizeof(sbyte));
            return (sbyte)Data[index];
        }

        public byte Get(int index)
        {
            AssertOffsetAndLength(index, sizeof(byte));
            return Data[index];
        }

#if UNSAFE_BYTEBUFFER

        // Unsafe but more efficient versions of Get*.
        public short GetShort(int offset)
        {
            return (short)GetUshort(offset);
        }

        public unsafe ushort GetUshort(int offset)
        {
            AssertOffsetAndLength(offset, sizeof(ushort));
            fixed (byte* ptr = Data)
            {
                return BitConverter.IsLittleEndian ? *(ushort*)(ptr + offset) : ReverseBytes(*(ushort*)(ptr + offset));
            }
        }

        public int GetInt(int offset)
        {
            return (int)GetUint(offset);
        }

        public unsafe uint GetUint(int offset)
        {
            AssertOffsetAndLength(offset, sizeof(uint));
            fixed (byte* ptr = Data)
            {
                return BitConverter.IsLittleEndian ? *(uint*)(ptr + offset) : ReverseBytes(*(uint*)(ptr + offset));
            }
        }

        public long GetLong(int offset)
        {
            return (long)GetUlong(offset);
        }

        public unsafe ulong GetUlong(int offset)
        {
            AssertOffsetAndLength(offset, sizeof(ulong));
            fixed (byte* ptr = Data)
            {
                return BitConverter.IsLittleEndian ? *(ulong*)(ptr + offset) : ReverseBytes(*(ulong*)(ptr + offset));
            }
        }

        public unsafe float GetFloat(int offset)
        {
            AssertOffsetAndLength(offset, sizeof(float));
            fixed (byte* ptr = Data)
            {
                if (BitConverter.IsLittleEndian)
                {
                    return *(float*)(ptr + offset);
                }

                uint uvalue = ReverseBytes(*(uint*)(ptr + offset));
                return *(float*)&uvalue;
            }
        }

        public unsafe double GetDouble(int offset)
        {
            AssertOffsetAndLength(offset, sizeof(double));
            fixed (byte* ptr = Data)
            {
                if (BitConverter.IsLittleEndian)
                {
                    return *(double*)(ptr + offset);
                }

                ulong uvalue = ReverseBytes(*(ulong*)(ptr + offset));
                return *(double*)&uvalue;
            }
        }

#else // !UNSAFE_BYTEBUFFER

// Slower versions of Get* for when unsafe code is not allowed.
        public short GetShort(int index)
        {
            return (short)ReadLittleEndian(index, sizeof(short));
        }

        public ushort GetUshort(int index)
        {
            return (ushort)ReadLittleEndian(index, sizeof(ushort));
        }

        public int GetInt(int index)
        {
            return (int)ReadLittleEndian(index, sizeof(int));
        }

        public uint GetUint(int index)
        {
            return (uint)ReadLittleEndian(index, sizeof(uint));
        }

        public long GetLong(int index)
        {
           return (long)ReadLittleEndian(index, sizeof(long));
        }

        public ulong GetUlong(int index)
        {
            return ReadLittleEndian(index, sizeof(ulong));
        }

        public float GetFloat(int index)
        {
            int i = (int)ReadLittleEndian(index, sizeof(float));
            inthelper[0] = i;
            Buffer.BlockCopy(inthelper, 0, floathelper, 0, sizeof(float));
            return floathelper[0];
        }

        public double GetDouble(int index)
        {
            ulong i = ReadLittleEndian(index, sizeof(double));
            

// There's Int64BitsToDouble but it uses unsafe code internally.
            ulonghelper[0] = i;
            Buffer.BlockCopy(ulonghelper, 0, doublehelper, 0, sizeof(double));
            return doublehelper[0];
        }
#endif // UNSAFE_BYTEBUFFER
    }
}
