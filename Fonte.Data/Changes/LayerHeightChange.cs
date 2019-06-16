
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerHeightChange : IChange
    {
        private readonly Layer _target;
        private float _value;

        public bool AffectsSelection => false;
        public bool IsShallow => false;

        public LayerHeightChange(Layer target, float value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._height;
            _target._height = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
