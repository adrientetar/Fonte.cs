/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Interfaces
{
    public interface ILocatable
    {
        float X { get; set; }
        float Y { get; set; }
    }

    public interface ILayerElement : ILocatable, ISelectable
    {
    }
}
