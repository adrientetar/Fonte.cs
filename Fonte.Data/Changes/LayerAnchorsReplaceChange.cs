
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerAnchorsReplaceChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Anchor _item;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerAnchorsReplaceChange(Layer parent, int index, Anchor item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._anchors;

            var oldItem = items[_index];
            items[_index] = _item;
            _item.Parent = _parent;
            _item = oldItem;
            _item.Parent = null;

            _parent.OnChange(this);
        }
    }
}
