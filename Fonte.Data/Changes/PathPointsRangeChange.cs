
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Collections.Generic;

    internal struct PathPointsRangeChange : IChange
    {
        private readonly Path _parent;
        private readonly int _index;
        private bool _insert;
        private List<Point> _items;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public PathPointsRangeChange(Path parent, int index, List<Point> items, bool insert)
        {
            _parent = parent;
            _index = index;
            _insert = insert;
            _items = items;
        }

        public void Apply()
        {
            var items = _parent._points;
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
