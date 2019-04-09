/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System.Collections.Generic;

    public class Iterable
    {
        public static IEnumerable<T> IterAt<T>(List<T> list, int index)
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
    }
}