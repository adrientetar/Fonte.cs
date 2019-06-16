
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct LayerAnchorsResetChange : IChange
    {
        private readonly Layer _parent;
        private List<Anchor> _items;

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
                var parent = _parent;
                items.AddRange(_items);
                _items.ForEach(item => item.Parent = parent);
                _items = null;
            }
            else
            {
                var removedItems = items.GetRange(0, items.Count);
                items.Clear();
                _items = removedItems;
                _items.ForEach(item => item.Parent = null);
            }

            _parent.OnChange(this);
        }
    }
}
