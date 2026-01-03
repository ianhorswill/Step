using System;
using System.Collections.Generic;
using System.Text;

namespace Step.Utilities
{
    public static class ArrayExtensions
    {
        public static T[] Slice<T>(this T[] array, int start, int count)
        {
            var result = new T[count];
            Array.Copy(array, start, result, 0, count);
            return result;
        }
    }
}
