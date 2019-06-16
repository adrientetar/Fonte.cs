
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class ChangeGroup : IChange, IChangeGroup
    {
        private readonly Action<int> _callback;
        private readonly List<IChange> _changes = new List<IChange>();
        private bool _disposed = false;
        private int _index;

        public int Count => _changes.Count;

        public bool AffectsSelection => true;
        // TODO: to avoid the >O(1) fetch, we can have _undoCounter similar to UndoStore's, but that
        // wouldn't play well with cloning... need to refactor members to an aggregate ref type (shared)
        // and an index (unique)
        public bool IsShallow => !Enumerable.Any(_changes, change => !change.IsShallow);

        public ChangeGroup(Action<int> callback, int index)
        {
            _callback = callback;
            _index = index;
        }

        public void Add(IChange item)
        {
            if (_disposed)
            {
                throw new InvalidOperationException($"Cannot add item to disposed {nameof(ChangeGroup)}");
            }

            _changes.Add(item);
        }

        public void Apply()
        {
            for (int i = _changes.Count - 1; i >= 0; i--)
            {
                _changes[i].Apply();
            }

            _changes.Reverse();
        }

        public ChangeGroup CloneWithIndex(int index)
        {
            if (_disposed)
            {
                throw new InvalidOperationException($"Cannot clone disposed {nameof(ChangeGroup)}");
            }

            ChangeGroup other;
            var oi = _index;
            try
            {
                _index = index;
                other = (ChangeGroup)MemberwiseClone();
            }
            finally
            {
                _index = oi;
            }
            return other;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _callback(_index);
                _disposed = true;
            }
        }
    }
}
