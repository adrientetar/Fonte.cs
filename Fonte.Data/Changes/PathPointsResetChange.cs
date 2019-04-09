
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct PathPointsResetChange : IChange
    {
        private readonly Path _parent;
        private List<Point> _items;

        bool Insert => _items != null;

        public bool ClearSelection => true;
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
