
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct LayerPathsResetChange : IChange
    {
        private readonly Layer _parent;
        private IList<Path> _items;

        bool Insert => _items != null;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public LayerPathsResetChange(Layer parent)
        {
            _parent = parent;
            _items = null;
        }

        public void Apply()
        {
            var items = _parent._paths;
            if (Insert)
            {
                items.AddRange(_items);
                foreach (var item in _items) { item.Parent = _parent; }
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
