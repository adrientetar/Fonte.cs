/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System.Collections.Generic;

    public static class ListExtensions
    {
        public static T Pop<T>(this List<T> list, int index)
        {
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }
    }
}
