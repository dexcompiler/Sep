﻿using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static nietras.SeparatedValues.SepParseMask;

namespace nietras.SeparatedValues.Test;

public partial class SepParseMaskTest
{
    [TestMethod]
    public void SepParseMaskTest_ParseSeparatorsLineEndingsMasks_Ordinary()
    {
        AssertParseSeparatorsLineEndingsMasks(";a;b\r\n;", new[] { 0, 2, 4 },
            rowLineEndingOffset: 0, expectedRowLineEndingOffset: 2,
            lineNumber: 2, expectedLineNumber: 3);

        AssertParseSeparatorsLineEndingsMasks("a;;b\r;", new[] { 1, 2, 4 },
            rowLineEndingOffset: 0, expectedRowLineEndingOffset: 1,
            lineNumber: 2, expectedLineNumber: 3);

        AssertParseSeparatorsLineEndingsMasks(";a;b\n;", new[] { 0, 2, 4 },
            rowLineEndingOffset: 0, expectedRowLineEndingOffset: 1,
            lineNumber: 2, expectedLineNumber: 3);

        AssertParseSeparatorsLineEndingsMasks(";aa;bbb;cccc;\n", new[] { 0, 3, 7, 12, 13 },
            rowLineEndingOffset: 0, expectedRowLineEndingOffset: 1,
            lineNumber: 2, expectedLineNumber: 3);

        AssertParseSeparatorsLineEndingsMasks(new string('a', s_nativeBitSize - 1) + "\r\n", new[] { s_nativeBitSize - 1 },
            rowLineEndingOffset: 0, expectedRowLineEndingOffset: 2,
            lineNumber: 2, expectedLineNumber: 3);
    }

    static void AssertParseSeparatorsLineEndingsMasks(string chars, int[] expected,
        int rowLineEndingOffset, int expectedRowLineEndingOffset,
        nuint quoting = 0, nuint expectedQuoting = 0,
        int lineNumber = -1, int expectedLineNumber = -1)
    {
        for (var i = 0; i < expected.Length; ++i) { expected[i] += CharsIndexOffset; }
        var separatorsMask = SeparatorsMaskFor(chars);
        var lineEndingsMask = LineEndingsMaskFor(chars);
        Span<int> colEnds = stackalloc int[s_nativeBitSize + 1];
        ref var start = ref colEnds[0];

        var charsIndex = CharsIndexOffset;

        ref var end = ref ParseSeparatorsLineEndingsMasks(
            separatorsMask, separatorsMask | lineEndingsMask,
            ref MemoryMarshal.GetReference<char>(chars), ref charsIndex, Separator,
            ref start, ref rowLineEndingOffset, ref lineNumber);

        AssertParseState(expected, colEnds, ref start, ref end,
            expectedRowLineEndingOffset, rowLineEndingOffset,
            expectedQuoting, quoting,
            expectedLineNumber, lineNumber);
    }

    static nuint SeparatorsMaskFor(ReadOnlySpan<char> chars)
    {
        nuint mask = 0;
        for (var i = 0; i < LengthForMask(chars.Length); i++)
        {
            if (chars[i] == Separator)
            {
                mask |= (nuint)1 << i;
            }
        }
        return mask;
    }

    static nuint LineEndingsMaskFor(ReadOnlySpan<char> chars)
    {
        nuint mask = 0;
        for (var i = 0; i < LengthForMask(chars.Length); i++)
        {
            var c = chars[i];
            if (c == SepDefaults.LineFeed || c == SepDefaults.CarriageReturn)
            {
                mask |= (nuint)1 << i;
            }
        }
        return mask;
    }
}
