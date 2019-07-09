
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct MasterGuidelinesResetChange : IChange
    {
        private readonly Master _parent;
        private IList<Guideline> _items;

        bool Insert => _items != null;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public MasterGuidelinesResetChange(Master parent)
        {
            _parent = parent;
            _items = null;
        }

        public void Apply()
        {
            var items = _parent._guidelines;
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

            //_parent.OnChange(this);
        }
    }
}
