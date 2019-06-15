
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct PointSelectedChange : IChange
    {
        private readonly Point _target;
        private bool _value;

        public bool ClearSelection => true;
        public bool IsShallow => true;

        public PointSelectedChange(Point target, bool value)
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
