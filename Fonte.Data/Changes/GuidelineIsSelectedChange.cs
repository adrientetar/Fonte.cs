
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct GuidelineIsSelectedChange : IChange
    {
        private readonly Guideline _target;
        private bool _value;

        public bool AffectsSelection => true;
        public bool IsShallow => true;

        public GuidelineIsSelectedChange(Guideline target, bool value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._isSelected;
            _target._isSelected = _value;
            _value = oldValue;

            (_target.Parent as Layer)?.OnChange(this);
        }
    }
}
