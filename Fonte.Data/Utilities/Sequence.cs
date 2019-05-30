/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System.Collections;
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

        public static int NextIndex(ICollection list, int index)
        {
            Debug.Assert(list.Count > 0);

            var value = index + 1;
            if (value >= list.Count)
            {
                value -= list.Count;
            }

            return value;
        }

        public static int PreviousIndex(ICollection list, int index)
        {
            Debug.Assert(list.Count > 0);

            var value = index - 1;
            if (value < 0)
            {
                value += list.Count;
            }

            return value;
        }
    }
}