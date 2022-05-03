using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Frotz.Other
{

    public static class ZMath
    {
        public static uint MakeInt(string chars)
        {
            Debug.Assert(chars.Length == 4, "Must be 4 characters.");
            byte[] bytes = Encoding.UTF8.GetBytes(chars);
            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MakeInt(Span<byte> bytes)
            => BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }
}