
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct PointTypeChange : IChange
    {
        private readonly Point _target;
        private PointType _value;

        public bool AffectsSelection => false;
        public bool IsShallow => false;

        public PointTypeChange(Point target, PointType value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._type;
            _target._type = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
