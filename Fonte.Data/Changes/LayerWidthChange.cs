
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerWidthChange : IChange
    {
        private Layer _target;
        private int _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public LayerWidthChange(Layer target, int value)
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
