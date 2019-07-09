
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct LayerPathsReplaceChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private IList<Path> _items;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public LayerPathsReplaceChange(Layer parent, int index, IList<Path> item)
        {
            _parent = parent;
            _index = index;
            _items = item;
        }

        public void Apply()
        {
            var items = _parent._paths;

            var oldItems = items.GetRange(_index, _items.Count);
            items.RemoveRange(_index, _items.Count);
            foreach (var item in oldItems) { item.Parent = null; }

            items.InsertRange(_index, _items);
            foreach (var item in oldItems) { item.Parent = _parent; }
            _items = oldItems;

            _parent.OnChange(this);
        }
    }
}
