
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct LayerNameChange : IChange
    {
        private readonly Layer _target;
        private string _value;

        public bool AffectsSelection => false;
        public bool IsShallow => false;

        public LayerNameChange(Layer target, string value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._name;
            _target._name = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
