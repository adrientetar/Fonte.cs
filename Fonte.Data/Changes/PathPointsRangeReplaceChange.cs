
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct PathPointsRangeReplaceChange : IChange
    {
        private readonly Path _parent;
        private readonly int _index;
        private List<Point> _items;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public PathPointsRangeReplaceChange(Path parent, int index, List<Point> item)
        {
            _parent = parent;
            _index = index;
            _items = item;
        }

        public void Apply()
        {
            var items = _parent._points;

            var oldItems = items.GetRange(_index, _items.Count);
            items.RemoveRange(_index, _items.Count);
            oldItems.ForEach(item => item.Parent = null);

            var parent = _parent;
            items.InsertRange(_index, _items);
            _items.ForEach(item => item.Parent = parent);
            _items = oldItems;

            _parent.OnChange(this);
        }
    }
}
