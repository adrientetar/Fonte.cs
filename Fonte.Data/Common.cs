/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using System;

    [Flags]
    public enum ChangeFlags
    {
        None = 0,
        Shape = 0b0001,
        ShapeOutline = 0b0011,
        Selection = 0b0100,
        Name = 0b1000,
    }
}
