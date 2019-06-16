
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct PathPointsChange : IChange
    {
        private readonly Path _parent;
        private readonly int _index;
        private Point _item;

        bool Insert => _item != null;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public PathPointsChange(Path parent, int index, Point item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._points;
            if (Insert)
            {
                items.Insert(_index, _item);
                _item.Parent = _parent;
                _item = null;
            }
            else
            {
                var item = items[_index];
                items.RemoveAt(_index);
                _item = item;
                _item.Parent = null;
            }

            _parent.OnChange(this);
        }
    }
}
