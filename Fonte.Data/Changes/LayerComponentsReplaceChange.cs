
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerComponentsReplaceChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Component _item;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerComponentsReplaceChange(Layer parent, int index, Component item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._components;

            var oldItem = items[_index];
            items[_index] = _item;
            _item.Parent = _parent;
            _item = oldItem;
            _item.Parent = null;

            _parent.OnChange(this);
        }
    }
}
