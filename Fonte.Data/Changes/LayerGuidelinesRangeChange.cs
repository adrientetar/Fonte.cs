
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct LayerGuidelinesRangeChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private bool _insert;
        private List<Guideline> _items;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerGuidelinesRangeChange(Layer parent, int index, List<Guideline> items, bool insert)
        {
            _parent = parent;
            _index = index;
            _insert = insert;
            _items = items;
        }

        public void Apply()
        {
            var items = _parent._guidelines;
            if (_insert)
            {
                var parent = _parent;
                items.InsertRange(_index, _items);
                _items.ForEach(item => item.Parent = parent);
            }
            else
            {
                items.RemoveRange(_index, _items.Count);
                _items.ForEach(item => item.Parent = null);
            }
            _insert = !_insert;

            _parent.OnChange(this);
        }
    }
}
