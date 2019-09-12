
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;
    using System.Diagnostics;

    internal struct MasterGuidelinesChange : IChange
    {
        private readonly Master _parent;
        private readonly int _index;
        private bool _insert;
        private readonly IList<Guideline> _items;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public MasterGuidelinesChange(Master parent, int index, IList<Guideline> items, bool insert)
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
                items.InsertRange(_index, _items);
                foreach (var item in _items) { Debug.Assert(item.Parent == null);
                                               item.Parent = _parent; }
            }
            else
            {
                items.RemoveRange(_index, _items.Count);
                foreach (var item in _items) { item.Parent = null; }
            }
            _insert = !_insert;

            //_parent.OnChange(this);
        }
    }
}
