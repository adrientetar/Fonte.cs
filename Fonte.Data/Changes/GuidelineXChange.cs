
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct GuidelineXChange : IChange
    {
        private Guideline _target;
        private float _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public GuidelineXChange(Guideline target, float value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._x;
            _target._x = _value;
            _value = oldValue;

            (_target.Parent as Layer)?.OnChange(this);
        }
    }
}
