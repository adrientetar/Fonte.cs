/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public class Sequence
    {
        public static IEnumerable<T> IterAt<T>(IList<T> list, int index)
        {
            for (int ix = index; ix < list.Count; ++ix)
            {
                yield return list[ix];
            }
            for (int ix = 0; ix < index; ++ix)
            {
                yield return list[ix];
            }
        }

        public static T NextItem<T>(IList<T> list, int index)
        {
            var count = list.Count;
            var n = 1;

            Debug.Assert(0 <= index && index < count);
            return list[index <= count - 1 - n ? index + n : index + n - count];
        }

        public static T PreviousItem<T>(IList<T> list, int index)
        {
            var count = list.Count;
            var n = 1;

            Debug.Assert(0 <= index && index < count);
            return list[index >= n ? index - n : index - n + count];
        }
    }
}