
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerPathsReplaceChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Path _item;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerPathsReplaceChange(Layer parent, int index, Path item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._paths;

            var oldItem = items[_index];
            items[_index] = _item;
            _item.Parent = _parent;
            _item = oldItem;
            _item.Parent = null;

            _parent.OnChange(this);
        }
    }
}
