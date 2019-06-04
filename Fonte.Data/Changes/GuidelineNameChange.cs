
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct GuidelineNameChange : IChange
    {
        private Guideline _target;
        private string _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public GuidelineNameChange(Guideline target, string value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._name;
            _target._name = _value;
            _value = oldValue;

            (_target.Parent as Layer)?.OnChange(this);
        }
    }
}
