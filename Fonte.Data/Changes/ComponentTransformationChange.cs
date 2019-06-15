
namespace Fonte.Data.Changes
{
    using Fonte.Data.Interfaces;

    using System.Numerics;

    internal struct ComponentTransformationChange : IChange
    {
        private readonly Component _target;
        private Matrix3x2 _value;

        public bool ClearSelection => false;
        public bool IsShallow => false;

        public ComponentTransformationChange(Component target, Matrix3x2 value)
        {
            _target = target;
            _value = value;
        }

        public void Apply()
        {
            var oldValue = _target._transformation;
            _target._transformation = _value;
            _value = oldValue;

            _target.Parent?.OnChange(this);
        }
    }
}
