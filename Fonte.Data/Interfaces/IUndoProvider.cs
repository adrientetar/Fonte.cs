// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data.Interfaces
{
    public interface IUndoProvider
    {
        bool CanRedo { get; }
        bool CanUndo { get; }
        bool HasOpenGroup { get; }

        IChangeGroup CreateUndoGroup();
        void Redo();
        void Undo();
    }

    internal interface IUndoStore : IUndoProvider
    {
        bool IsEnabled { get; set; }

        void OnUndoGroupDisposed(int index);
    }
}
