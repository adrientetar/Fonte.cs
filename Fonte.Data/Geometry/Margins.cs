// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Fonte.Data.Geometry
{
    using System;
    using System.Globalization;
    using System.Numerics;

    // Same as Windows.UI.Xaml.Thickness, but:
    // - name change
    // - uniform ctor is removed (doesn't make much sense)
    // - uses float instead of double
    // - adds Empty
    public struct Margins
    {
        private float _left;
        private float _top;
        private float _right;
        private float _bottom;

        private const float EmptyLeft = float.NegativeInfinity;
        private const float EmptyTop = float.PositiveInfinity;
        private const float EmptyRight = float.NegativeInfinity;
        private const float EmptyBottom = float.PositiveInfinity;

        private static readonly string ArgumentOutOfRange_IllegalNegInfinity = "Negative infinity is illegal.";

        public float Left
        {
            get { return _left; }
            set {
                if (value == EmptyLeft)
                    throw new ArgumentOutOfRangeException(nameof(Left), ArgumentOutOfRange_IllegalNegInfinity);

                _left = value;
            }
        }

        public float Top
        {
            get { return _top; }
            set { _top = value; }
        }

        public float Right
        {
            get { return _right; }
            set { _right = value; }
        }

        public float Bottom
        {
            get { return _bottom; }
            set { _bottom = value; }
        }

        public static Margins Empty { get; } = CreateEmptyMargins();

        public bool IsEmpty
        {
            get { return _left == EmptyLeft; }
        }

        public Margins(float left, float top, float right, float bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
        }

        static Margins CreateEmptyMargins()
        {
            Margins margins = new Margins
            {
                _left = EmptyLeft,
                _top = EmptyTop,
                _right = EmptyRight,
                _bottom = EmptyBottom
            };

            return margins;
        }

        public override string ToString()
        {
            var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

            return string.Format("{1:}{0}{2:}{0}{3:}{0}{4:}",
                                 separator,
                                 _left,
                                 _top,
                                 _right,
                                 _bottom);
        }

        public override bool Equals(object obj)
        {
            if (obj is Margins otherObj)
            {
                return (this == otherObj);
            }
            return (false);
        }

        public bool Equals(Margins thickness)
        {
            return (this == thickness);
        }

        public override int GetHashCode()
        {
            return _left.GetHashCode() ^ _top.GetHashCode() ^ _right.GetHashCode() ^ _bottom.GetHashCode();
        }

        public static bool operator ==(Margins t1, Margins t2)
        {
            return t1._left == t2._left && t1._top == t2._top && t1._right == t2._right && t1._bottom == t2._bottom;
        }

        public static bool operator !=(Margins t1, Margins t2)
        {
            return (!(t1 == t2));
        }
    }
}