﻿using System;
using System.Text;

namespace Frotz;

public static class Extensions
{
    public static StringBuilder Remove(this StringBuilder sb, int offset, int length)
    {
        return sb.Remove(offset, length);
    }

    public static bool Matches(this Span<byte> bytes, ReadOnlySpan<char> chars)
        => Matches((ReadOnlySpan<byte>)bytes, chars);

    public static bool Matches(this ReadOnlySpan<byte> bytes, ReadOnlySpan<char> chars)
    {
        if (bytes.Length != chars.Length)
            return false;

        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != chars[i])
                return false;
        }

        return true;
    }
}
