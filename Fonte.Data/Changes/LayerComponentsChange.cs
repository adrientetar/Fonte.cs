
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerComponentsChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Component _item;

        bool Insert => _item != null;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerComponentsChange(Layer parent, int index, Component item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._components;
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
