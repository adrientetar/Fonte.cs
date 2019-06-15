
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct AnchorSelectedChange : IChange
    {
        private readonly Anchor _target;
        private bool _value;

        public bool ClearSelection => true;
        public bool IsShallow => true;

        public AnchorSelectedChange(Anchor target, bool value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._selected;
            _target._selected = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
