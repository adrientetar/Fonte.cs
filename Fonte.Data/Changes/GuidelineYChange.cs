
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct GuidelineYChange : IChange
    {
        private readonly Guideline _target;
        private float _value;

        public bool AffectsSelection => false;
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

            (_target.Parent as Layer)?.OnChange(this);
        }
    }
}
