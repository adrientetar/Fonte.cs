
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct PointIsSmoothChange : IChange
    {
        private readonly Point _target;
        private bool _value;

        public bool AffectsSelection => false;
        public bool IsShallow => false;

        public PointIsSmoothChange(Point target, bool value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._isSmooth;
            _target._isSmooth = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
