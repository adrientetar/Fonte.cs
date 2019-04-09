
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct PointSmoothChange : IChange
    {
        private Point _target;
        private bool _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public PointSmoothChange(Point target, bool value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._smooth;
            _target._smooth = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
