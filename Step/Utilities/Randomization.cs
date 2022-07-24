using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Step.Utilities
{
    /// <summary>
    /// Utilities for generating random numbers and permutations.
    /// </summary>
    public static class Randomization
    {
        /// <summary>
        /// Random number generator used by the Step interpreter.
        /// Setting its seed will fix the behavior of the system.
        /// </summary>
        public static readonly Random Random = new Random();

        /// <summary>
        /// Generate a random integer in the range [low, high]
        /// </summary>
        /// <param name="low">Smallest number to allow</param>
        /// <param name="high">Largest number to allow</param>
        /// <returns></returns>
        public static int IntegerInclusive(int low, int high) => Random.Next(low, high + 1);

        /// <summary>
        /// Generate a random integer in the range [low, high), that is, low can be generated but not high.
        /// </summary>
        /// <param name="low">Smallest allowed number</param>
        /// <param name="high">Largest allowed number plus one</param>
        /// <returns>Generated number</returns>
        public static int IntegerExclusive(int low, int high) => Random.Next(low, high);

        /// <summary>
        /// Make a randomly permuted copy of sequence
        /// </summary>
        public static T[] Shuffle<T>(this IList<T> sequence)
        {
            var result = sequence.ToArray();
            for (var i = result.Length - 1; i > 0; i--)
            {
                var index = Random.Next(i + 1);
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

        private static readonly uint[] Primes = new[]
        {
            1u, 2u, 3u, 5u, 7u, 11u, 13u, 17u, 19u, 23u, 29u, 31u, 37u, 41u, 43u, 53u, 59u, 61u, 67u, 71u, 73u, 79u,
            83u, 89u, 97u
        };

        private static readonly int[] HighestPrimeIndex = new int[100];

        static Randomization()
        {
            var index = 0;
            // Set HighestPrimeIndex[i] = index of largest prime in Primes that less than i
            for (int i = 1; i < HighestPrimeIndex.Length; i++)
            {
                if (index < Primes.Length - 1 && i > Primes[index + 1])
                    // Next prime
                    index++;
                HighestPrimeIndex[i] = index;
            }
        }

        private static readonly Random Rng = new Random();

        /// <summary>
        /// Enumerates the elements of list in a random order.
        /// This is good enough to seem random to a human, but actually only generates O(n^2/log n)
        /// possible permutations, which is much less than the n! actual permutations.
        /// The reason for this limitation is that it lets us enumerate using constant space.
        /// </summary>
        /// <param name="list">List to enumerate</param>
        /// <typeparam name="T">Type of the list element</typeparam>
        /// <returns>Random permutation of list</returns>
        public static IEnumerable<T> BadShuffle<T>(this IList<T> list)
        {
            var length = (uint) list.Count;
            if (length == 0)
                yield break;

            // Pick an random starting point and step size
            // Step size needs to be relatively prime to length if we're
            // to hit all the elements, so we always choose a prime number
            // for the step

            var position = Rng.Next() % length;

            // Set step = random prime less than length (or 1 if length == 1)
            var maxPrimeIndex = Primes.Length - 1;
            if (length < HighestPrimeIndex.Length)
                maxPrimeIndex = HighestPrimeIndex[length];
            var step = Primes[Rng.Next() % (maxPrimeIndex + 1)];

            for (uint i = 0; i < length; i++)
            {
                yield return list[(int) position];
                position = (position + step) % length;
            }
        }

        /// <summary>
        /// Enumerates the elements of list in a random order.
        /// This is good enough to seem random to a human, but actually only generates O(n^2/log n)
        /// possible permutations, which is much less than the n! actual permutations.
        /// The reason for this limitation is that it lets us enumerate using constant space.
        /// </summary>
        /// <param name="list">List to enumerate</param>
        /// <returns>Random permutation of list</returns>
        public static IEnumerable BadShuffle(this IList list)
        {
            var length = (uint)list.Count;
            if (length == 0)
                yield break;

            // Pick an random starting point and step size
            // Step size needs to be relatively prime to length if we're
            // to hit all the elements, so we always choose a prime number
            // for the step

            var position = Rng.Next() % length;

            // Set step = random prime less than length (or 1 if length == 1)
            var maxPrimeIndex = Primes.Length - 1;
            if (length < HighestPrimeIndex.Length)
                maxPrimeIndex = HighestPrimeIndex[length];
            var step = Primes[Rng.Next() % (maxPrimeIndex + 1)];

            for (uint i = 0; i < length; i++)
            {
                yield return list[(int)position];
                position = (position + step) % length;
            }
        }

        /// <summary>
        /// Returns either the original list, if shouldShuffle=false,
        /// or a random permutation generated with BadShuffle(), if shouldShuffle=true.
        /// </summary>
        /// <param name="list">List to enumerate</param>
        /// <param name="shouldShuffle">Whether to randomize the order</param>
        /// <typeparam name="T">Element type</typeparam>
        /// <returns>Enumeration of the elements</returns>
        // ReSharper disable once UnusedMember.Global
        public static IEnumerable<T> MaybeShuffle<T>(this IList<T> list, bool shouldShuffle) =>
            shouldShuffle ? list.BadShuffle() : list;
    }
}
