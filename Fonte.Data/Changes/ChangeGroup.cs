
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System;
    using System.Collections.Generic;

    internal class ChangeGroup : IChange, IChangeGroup
    {
        private readonly Action<int> _callback;
        private readonly List<IChange> _changes = new List<IChange>();
        private bool _disposed = false;
        private readonly int _index;

        public int Count => _changes.Count;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public bool IsTopLevel => _index == 0;

        public ChangeGroup(Action<int> callback, int index)
        {
            _callback = callback;
            _index = index;
        }

        public void Add(IChange item)
        {
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

        public void Clear()
        {
            _changes.Clear();
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
