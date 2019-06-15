
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct AnchorNameChange : IChange
    {
        private readonly Anchor _target;
        private string _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public AnchorNameChange(Anchor target, string value)
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
