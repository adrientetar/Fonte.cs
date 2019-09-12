
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;
    using System.Diagnostics;

    internal struct LayerAnchorsResetChange : IChange
    {
        private readonly Layer _parent;
        private IList<Anchor> _items;

        bool Insert => _items != null;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public LayerAnchorsResetChange(Layer parent)
        {
            _parent = parent;
            _items = null;
        }

        public void Apply()
        {
            var items = _parent._anchors;
            if (Insert)
            {
                items.AddRange(_items);
                foreach (var item in _items) { Debug.Assert(item.Parent == null);
                                               item.Parent = _parent; }
                _items = null;
            }
            else
            {
                var removedItems = items.GetRange(0, items.Count);
                items.Clear();
                _items = removedItems;
                foreach (var item in _items) { item.Parent = null; }
            }

            _parent.OnChange(this);
        }
    }
}
