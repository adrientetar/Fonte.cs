// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data.Interfaces
{
    using System;

    interface IChange
    {
        // TODO: this could be static
        bool AffectsSelection { get; }
        bool IsShallow { get; }

        void Apply();
    }

    // Note: IChangeGroup is never shallow
    // if you have only shallow changes, don't group them.
    public interface IChangeGroup : IDisposable
    {
        int Count { get; }

        void Reset();
    }
}
