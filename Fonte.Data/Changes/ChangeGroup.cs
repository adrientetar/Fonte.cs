
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System;
    using System.Collections.Generic;

    class ChangeGroupInner
    {
        private readonly IUndoStore _undoStore;
        private readonly List<IChange> _changes = new List<IChange>();
        private int _undoCounter = 0;

        public int Count => _changes.Count;
        public bool IsShallow => _undoCounter <= 0;

        public ChangeGroupInner(IUndoStore undoStore)
        {
            _undoStore = undoStore;
        }

        public void Add(IChange item)
        {
            _changes.Add(item);

            if (!item.IsShallow)
            {
                ++_undoCounter;
            }
        }

        public void Apply()
        {
            for (int i = _changes.Count - 1; i >= 0; i--)
            {
                _changes[i].Apply();
            }

            _changes.Reverse();
        }

        public void Reset()
        {
            try
            {
                _undoStore.IsEnabled = false;

                for (int i = _changes.Count - 1; i >= 0; i--)
                {
                    _changes[i].Apply();
                }

                _changes.Clear();
                _undoCounter = 0;
            }
            finally
            {
                _undoStore.IsEnabled = true;
            }
        }

        public void Dispose(int index)
        {
            _undoStore.OnUndoGroupDisposed(index);
        }
    }

    internal struct ChangeGroup : IChange, IChangeGroup
    {
        private readonly ChangeGroupInner _inner;
        private readonly int _index;
        private bool _disposed;

        public int Count => _inner.Count;

        public bool AffectsSelection => true;
        public bool IsShallow => _inner.IsShallow;

        public ChangeGroup(IUndoStore undoStore, int index) : this(new ChangeGroupInner(undoStore), index)
        {
        }

        ChangeGroup(ChangeGroupInner inner, int index)
        {
            _inner = inner;
            _index = index;
            _disposed = false;
        }

        public void Add(IChange item)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"Cannot add item to disposed {nameof(ChangeGroup)}.");
            }

            _inner.Add(item);
        }

        public void Apply() => _inner.Apply();

        public ChangeGroup CloneWithIndex(int index)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException($"Cannot clone disposed {nameof(ChangeGroup)}.");
            }

            return new ChangeGroup(_inner, index);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _inner.Dispose(_index);
                _disposed = true;
            }
        }

        public void Reset() => _inner.Reset();
    }
}
