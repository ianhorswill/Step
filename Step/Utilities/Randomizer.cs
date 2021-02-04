using System;
using System.Collections.Generic;
using System.Linq;

namespace Step.Utilities
{
    internal static class Randomizer
    {
        public static readonly Random Random = new Random();

        public static int IntegerInclusive(int low, int high) => Random.Next(low, high + 1);
        public static int IntegerExclusive(int low, int high) => Random.Next(low, high);

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

        private readonly struct ShuffleData
        {
            public readonly int Index;
            public readonly float Key;

            public ShuffleData(int index, float key)
            {
                this.Index = index;
                this.Key = key;
            }
        }

        /// <summary>
        /// Return a shuffled version of the elements in sequence, given the specified weights.
        /// This is based on Weighted Random Sampling (2005; Efraimidis, Spirakis), Encyclopedia of Algorithms.
        /// http://utopia.duth.gr/~pefraimi/research/data/2007EncOfAlg.pdf
        /// </summary>
        public static T[] WeightedShuffle<T>(this IList<T> sequence, Func<T, float> weight)
        {
            var data = new ShuffleData[sequence.Count];
            for (var i = 0; i < data.Length; i++)
                data[i] = new ShuffleData(i, (float) Math.Pow(Random.NextDouble(), 1 / weight(sequence[i])));
            Array.Sort(data, (a, b) => -a.Key.CompareTo(b.Key));
            
            var result = new T[sequence.Count];
            for (var i = 0; i < result.Length; i++)
                result[i] = sequence[data[i].Index];
            
            return result;
        }
    }
}
