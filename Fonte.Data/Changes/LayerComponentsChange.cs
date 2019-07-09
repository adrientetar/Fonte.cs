
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct LayerComponentsChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private bool _insert;
        private readonly IList<Component> _items;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public LayerComponentsChange(Layer parent, int index, IList<Component> items, bool insert)
        {
            _parent = parent;
            _index = index;
            _insert = insert;
            _items = items;
        }

        public void Apply()
        {
            var items = _parent._components;
            if (_insert)
            {
                items.InsertRange(_index, _items);
                foreach (var item in _items) { item.Parent = _parent; }
            }
            else
            {
                items.RemoveRange(_index, _items.Count);
                foreach (var item in _items) { item.Parent = null; }
            }
            _insert = !_insert;

            _parent.OnChange(this);
        }
    }
}
