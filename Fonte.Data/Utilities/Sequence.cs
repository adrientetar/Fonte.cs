// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data.Utilities
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class Sequence
    {
        public static IEnumerable<T> IterAt<T>(IList<T> list, int index, bool inclusive = false)
        {
            for (int ix = index; ix < list.Count; ++ix)
            {
                yield return list[ix];
            }
            for (int ix = 0; ix < (inclusive ? index + 1 : index); ++ix)
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