using System;
using System.Collections.Generic;
using System.Linq;

namespace Nuart
{
    public static class Utility
    {
        public static bool GetBitValue(byte data, int index)
        {
            if (index < 0 || index > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return (data & (1 << index)) != 0;
        }

        public static bool[] GetBitValues(byte data)
        {
            var values = new bool[8];

            for (var i = 0; i < 8; i++)
            {
                values[i] = GetBitValue(data, i);
            }
            return values;
        }

        public static uint[] ToHostUInt32(IList<byte> bytes, ByteOrder4 byteOrder)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Count % 4 != 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            bool isLittleEndian = BitConverter.IsLittleEndian;
            uint[] uints = new uint[bytes.Count / 4];

            for (var i = 0; i < bytes.Count; i++)
            {
                if (byteOrder == ByteOrder4.ABCD)
                {
                    uints[i / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }, 0);
                }
                else if (byteOrder == ByteOrder4.DCBA)
                {
                    uints[i / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }, 0);
                }
                else if (byteOrder == ByteOrder4.BADC)
                {
                    uints[i / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }, 0);
                }
                else if (byteOrder == ByteOrder4.CDAB)
                {
                    uints[i / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[i + 3], bytes[i + 2], bytes[i + 1], bytes[i] }, 0);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return uints;
        }

        public static uint[] ToHostUInt32(IList<byte> bytes, int startIndex, int length, ByteOrder4 byteOrder)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Count % 4 != 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (startIndex < 0)
            {
            }

            if (length < 0)
            {
            }

            if (startIndex + length > 0)
            {
            }

            bool isLittleEndian = BitConverter.IsLittleEndian;
            uint[] uints = new uint[bytes.Count / 4];

            for (int i = 0; i < length; i += 4)
            {
                startIndex += i;
                if (byteOrder == ByteOrder4.ABCD)
                {
                    uints[startIndex / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[startIndex], bytes[startIndex + 1], bytes[startIndex + 2], bytes[startIndex + 3] }, 0);
                }
                else if (byteOrder == ByteOrder4.DCBA)
                {
                    uints[startIndex / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[startIndex], bytes[startIndex + 1], bytes[startIndex + 2], bytes[startIndex + 3] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex] }, 0);
                }
                else if (byteOrder == ByteOrder4.BADC)
                {
                    uints[startIndex / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[startIndex], bytes[startIndex + 1], bytes[startIndex + 2], bytes[startIndex + 3] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex] }, 0);
                }
                else if (byteOrder == ByteOrder4.CDAB)
                {
                    uints[startIndex / 4] = isLittleEndian
                        ? BitConverter.ToUInt32(new byte[4] { bytes[startIndex], bytes[startIndex + 1], bytes[startIndex + 2], bytes[startIndex + 3] }, 0)
                        : BitConverter.ToUInt32(new byte[4] { bytes[startIndex + 3], bytes[startIndex + 2], bytes[startIndex + 1], bytes[startIndex] }, 0);
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return uints;
        }

        public static int[] ToHostInt32(IList<byte> bytes, int startIndex, int length, ByteOrder4 byteOrder) =>
            ToHostUInt32(bytes, startIndex, length, byteOrder).Select(item => (int)item).ToArray();

        public static int[] ToHostInt32(IList<byte> bytes, ByteOrder4 byteOrder)
        {
            return ToHostInt32(bytes, 0, bytes.Count, byteOrder);
        }

        public static float[] ToHostFloat(IList<byte> bytes, int startIndex, int length, ByteOrder4 byteOrder) =>
            ToHostUInt32(bytes, startIndex, length, byteOrder).Select(item => (float)item).ToArray();

        public static float[] ToHostFloat(IList<byte> bytes, ByteOrder4 byteOrder) =>
            ToHostFloat(bytes, 0, bytes.Count, byteOrder);
    }
}