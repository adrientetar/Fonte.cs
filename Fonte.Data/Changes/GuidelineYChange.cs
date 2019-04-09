
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct GuidelineYChange : IChange
    {
        private Guideline _target;
        private float _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public GuidelineYChange(Guideline target, float value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._y;
            _target._y = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
