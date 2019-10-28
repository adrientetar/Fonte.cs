// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

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
