﻿using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace nietras.SeparatedValues;

static class SepArrayExtensions
{
    internal static int CheckFreeMaybeMoveMaybeDoubleLength<T>(ref T[] array, ref int start, ref int end,
        int minimumFreeLength, int paddingLengthToClear)
    {
        A.Assert(end >= start);

        var tailFreeLength = array.Length - end - paddingLengthToClear;
        if (tailFreeLength < minimumFreeLength)
        {
            var totalFreeLength = tailFreeLength + start;
            if (totalFreeLength >= minimumFreeLength)
            {
                return array.MoveDataToStart(ref start, ref end, paddingLengthToClear);
            }
            else
            {
                (array, var offset) = array.DoubleCapacityAndMove(start, end, paddingLengthToClear);
                start -= offset;
                end -= offset;
                return offset;
            }
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static int MoveDataToStart<T>(this T[] array, ref int start, ref int end,
        int paddingLengthToClear)
    {
        if (start > 0)
        {
            //L.WriteLine($"{nameof(MoveDataToStart)} Length:{array.Length} start:{start} end:{end}");
            var lengthToCopy = end - start;
            if (lengthToCopy > 0)
            { Array.Copy(array, start, array, 0, lengthToCopy); }
            var offset = start;
            end -= offset;
            start = 0;
            if (paddingLengthToClear > 0) { ClearPaddingAfterData(array, end, paddingLengthToClear); }
            return offset;
        }
        return 0;
    }

    // Should be called rarely after things settle, so never inline
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (T[] Array, int Offset) DoubleCapacityAndMove<T>(
        this T[] array, int start, int end, int paddingLengthToClear)
    {
        var newArray = ArrayPool<T>.Shared.Rent(array.Length * 2);
        var dataLength = end - start;
        Array.Copy(array, start, newArray, 0, dataLength);
        ArrayPool<T>.Shared.Return(array);
        if (paddingLengthToClear > 0) { ClearPaddingAfterData(newArray, dataLength, paddingLengthToClear); }
        return (newArray, start);
    }

    internal static void ClearPaddingAfterData<T>(this T[] array, int end, int paddingLength) =>
        Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref array[end]), 0, (uint)(paddingLength * Unsafe.SizeOf<T>()));

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    internal static void CheckPaddingAndIsZero<T>(this T[] array, int end, int minimumPaddingLength)
        where T : IEquatable<T>
    {
        array.CheckPadding(end, minimumPaddingLength);
        // Only check if padding is zero if array contains data, and end is not
        // the beginning. Otherwise, we might be checking padding that has not
        // been cleared yet.
        if (end > 0)
        {
            // Check padding is zero/default
            for (var i = 0; i < minimumPaddingLength; i++)
            {
                var value = array[end + i];
                A.Assert(value.Equals(default), $"Padding not zero at {end + i} but {value}");
            }
        }
    }

    [Conditional("DEBUG")]
    [ExcludeFromCodeCoverage]
    internal static void CheckPadding<T>(this T[] array, int end, int minimumPaddingLength)
    {
        var paddingLength = (array.Length - end);
        A.Assert(paddingLength >= minimumPaddingLength,
                     $"Padding length {paddingLength} less than minimum {minimumPaddingLength}");
    }

}
