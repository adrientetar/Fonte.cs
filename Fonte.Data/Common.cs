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
        Outline = 0b001,
        Selection = 0b010,
        SelectionRemove = 0b110,
    }
}
