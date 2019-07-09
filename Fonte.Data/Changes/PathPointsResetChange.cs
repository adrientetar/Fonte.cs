
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct PathPointsResetChange : IChange
    {
        private readonly Path _parent;
        private IList<Point> _items;

        bool Insert => _items != null;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public PathPointsResetChange(Path parent)
        {
            _parent = parent;
            _items = null;
        }

        public void Apply()
        {
            var items = _parent._points;
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
