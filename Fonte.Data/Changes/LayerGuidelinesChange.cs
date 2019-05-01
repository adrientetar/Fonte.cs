
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerGuidelinesChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Guideline _item;

        bool Insert => _item != null;

        public bool ClearSelection => true;
        public bool IsShallow => false;

        public LayerGuidelinesChange(Layer parent, int index, Guideline item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._guidelines;
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
