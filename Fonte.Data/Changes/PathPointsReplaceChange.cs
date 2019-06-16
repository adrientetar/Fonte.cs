
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct PathPointsReplaceChange : IChange
    {
        private readonly Path _parent;
        private readonly int _index;
        private Point _item;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public PathPointsReplaceChange(Path parent, int index, Point item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._points;

            var oldItem = items[_index];
            items[_index] = _item;
            _item.Parent = _parent;
            _item = oldItem;
            _item.Parent = null;

            _parent.OnChange(this);
        }
    }
}
