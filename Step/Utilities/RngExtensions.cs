using System;
using System.Collections.Generic;
using System.Linq;

namespace Step.Utilities
{
    internal static class RngExtensions
    {
        private static readonly Random Random = new Random();
        /// <summary>
        /// Make a randomly permuted copy of sequence
        /// </summary>
        public static T[] Shuffle<T>(this IList<T> sequence)
        {
            var result = sequence.ToArray();
            for (var i = result.Length - 1; i > 0; i--)
            {
                var index = Random.Next(i+1);
                var temp = result[i];
                result[i] = result[index];
                result[index] = temp;
            }

            return result;
        }
    }
}
