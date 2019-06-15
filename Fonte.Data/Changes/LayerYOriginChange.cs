
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerYOriginChange : IChange
    {
        private readonly Layer _target;
        private float _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public LayerYOriginChange(Layer target, float value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._yOrigin;
            _target._yOrigin = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
