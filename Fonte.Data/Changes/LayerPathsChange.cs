
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerPathsChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Path _item;

        bool Insert => _item != null;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public LayerPathsChange(Layer parent, int index, Path item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._paths;
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
