﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Akka.Event;
using Debug = System.Diagnostics.Debug;

namespace Akka.Remote.Artery.Utils
{
    internal static class Extensions
    {
        /// <summary>
        /// Bitwise rotate left an unsigned integer
        /// </summary>
        /// <param name="i"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(this uint i, int distance)
            => (i << distance) | (i >> -distance);

        /// <summary>
        /// Bitwise rotate right an unsigned integer
        /// </summary>
        /// <param name="i"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateRight(this uint i, int distance)
            => (i >> distance) | (i << -distance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOfTwo(this int value)
        {
            // Taken from https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2

            Debug.Assert(value >= 0);

            // If the number is already a power of 2, we want to round to itself.
            value--;

            // Propogate 1-bits right: if the highest bit set is @ position n,
            // then all of the bits to the right of position n will become set.
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;

            // This yields a number of the form 2^N - 1.
            // Add 1 to get a power of 2 with the bit set @ position n + 1.
            return value + 1;
        }

        public static bool NonFatal(this Exception e)
        {
            switch (e)
            {
                case null: return true;
                case SystemException _:
                    return false;
                default: return true;
            }
        }
    }
}
