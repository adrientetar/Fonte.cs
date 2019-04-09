
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct AnchorXChange : IChange
    {
        private Anchor _target;
        private float _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public AnchorXChange(Anchor target, float value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._x;
            _target._x = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
