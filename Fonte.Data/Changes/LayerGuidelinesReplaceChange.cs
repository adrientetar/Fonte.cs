
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerGuidelinesReplaceChange : IChange
    {
        private readonly Layer _parent;
        private readonly int _index;
        private Guideline _item;

        public bool AffectsSelection => true;
        public bool IsShallow => false;

        public LayerGuidelinesReplaceChange(Layer parent, int index, Guideline item)
        {
            _parent = parent;
            _index = index;
            _item = item;
        }

        public void Apply()
        {
            var items = _parent._guidelines;

            var oldItem = items[_index];
            items[_index] = _item;
            _item.Parent = _parent;
            _item = oldItem;
            _item.Parent = null;

            _parent.OnChange(this);
        }
    }
}
