
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerWidthChange : IChange
    {
        private readonly Layer _target;
        private float _value;

        public bool AffectsSelection => false;
        public bool IsShallow => false;

        public LayerWidthChange(Layer target, float value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._width;
            _target._width = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
