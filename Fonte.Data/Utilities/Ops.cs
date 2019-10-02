/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System.Diagnostics;

    public static class Ops
    {
        public static float Modulo(float x, float m)
        {
            Debug.Assert(m >= 0);
            var r = x % m;

            return r < 0 ? r + m : r;
        }
    }
}
