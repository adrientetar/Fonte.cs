/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Interfaces
{
    using System.Numerics;

    public interface ILocatable
    {
        float X { get; set; }
        float Y { get; set; }

        Vector2 ToVector2();
    }
}
