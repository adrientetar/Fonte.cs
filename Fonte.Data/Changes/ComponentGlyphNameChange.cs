
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    internal struct ComponentGlyphNameChange : IChange
    {
        private Component _target;
        private string _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public ComponentGlyphNameChange(Component target, string value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._glyphName;
            _target._glyphName = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
