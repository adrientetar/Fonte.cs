﻿
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct LayerGuidelinesResetChange : IChange
    {
        private readonly Layer _parent;
        private List<Guideline> _items;

        bool Insert => _items != null;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerGuidelinesResetChange(Layer parent)
        {
            _parent = parent;
            _items = null;
        }

        public void Apply()
        {
            var items = _parent._guidelines;
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
