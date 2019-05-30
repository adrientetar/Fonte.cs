// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Fonte.Data.Geometry
{
    using System;
    using System.Globalization;
    using System.Numerics;

    // Same as Windows.Foundation.Rect, but:
    // - Bottom/Top aren't inverted to account for topmost y origin
    // - methods return float rather than double, and consequently take Vector2 rather than Point
    // - location/size ctor is removed
    //
    // Foundation is used by Windows UI for things like window geometry, we're doing font geometry
    // so it makes sense to redefine whatsoever.
    public struct Rect
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        private const float EmptyX = float.PositiveInfinity;
        private const float EmptyY = float.PositiveInfinity;
        private const float EmptyWidth = float.NegativeInfinity;
        private const float EmptyHeight = float.NegativeInfinity;

        private static readonly string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

        public float X
        {
            get => _x;
            set { _x = value; }
        }

        public float Y
        {
            get => _y;
            set { _y = value; }
        }

        public float Width
        {
            get => _width;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), ArgumentOutOfRange_NeedNonNegNum);

                _width = value;
            }
        }

        public float Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), ArgumentOutOfRange_NeedNonNegNum);

                _height = value;
            }
        }

        public float Left
        {
            get { return _x; }
        }

        public float Bottom
        {
            get { return _y; }
        }

        public float Right
        {
            get
            {
                if (IsEmpty)
                {
                    return float.NegativeInfinity;
                }

                return _x + _width;
            }
        }

        public float Top
        {
            get
            {
                if (IsEmpty)
                {
                    return float.NegativeInfinity;
                }

                return _y + _height;
            }
        }

        public static Rect Empty { get; } = CreateEmptyRect();

        public bool IsEmpty
        {
            get { return _width < 0; }
        }

        public Rect(Vector2 point1, Vector2 point2)
        {
            _x = Math.Min(point1.X, point2.X);
            _y = Math.Min(point1.Y, point2.Y);

            _width = Math.Max(Math.Max(point1.X, point2.X) - _x, 0);
            _height = Math.Max(Math.Max(point1.Y, point2.Y) - _y, 0);
        }

        public Rect(float x, float y, float width, float height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), ArgumentOutOfRange_NeedNonNegNum);

            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        public bool Contains(Vector2 point)
        {
            return ((point.X >= X) && (point.X - Width <= X) &&
                    (point.Y >= Y) && (point.Y - Height <= Y));
        }

        public void Intersect(Rect rect)
        {
            if (!IntersectsWith(rect))
            {
                this = Empty;
            }
            else
            {
                float left = Math.Max(X, rect.X);
                float bottom = Math.Max(Y, rect.Y);

                //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                Width = Math.Max(Math.Min(X + Width, rect.X + rect.Width) - left, 0);
                Height = Math.Max(Math.Min(Y + Height, rect.Y + rect.Height) - bottom, 0);

                X = left;
                Y = bottom;
            }
        }

        public void Union(Vector2 point)
        {
            Union(new Rect(point, point));
        }

        public void Union(Rect rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                float left = Math.Min(Left, rect.Left);
                float bottom = Math.Min(Bottom, rect.Bottom);


                // We need this check so that the math does not result in NaN
                if ((rect.Width == float.PositiveInfinity) || (Width == float.PositiveInfinity))
                {
                    Width = float.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    float maxRight = Math.Max(Right, rect.Right);
                    Width = Math.Max(maxRight - left, 0);
                }

                // We need this check so that the math does not result in NaN
                if ((rect.Height == float.PositiveInfinity) || (Height == float.PositiveInfinity))
                {
                    Height = float.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    float maxTop = Math.Max(Top, rect.Top);
                    Height = Math.Max(maxTop - bottom, 0);
                }

                X = left;
                Y = bottom;
            }
        }

        internal bool IntersectsWith(Rect rect)
        {
            if (Width < 0 || rect.Width < 0)
            {
                return false;
            }

            return (rect.X <= X + Width) &&
                   (rect.X + rect.Width >= X) &&
                   (rect.Y <= Y + Height) &&
                   (rect.Y + rect.Height >= Y);
        }

        static Rect CreateEmptyRect()
        {
            Rect rect = new Rect
            {
                _x = EmptyX,
                _y = EmptyY,
                _width = EmptyWidth,
                _height = EmptyHeight
            };

            return rect;
        }

        public Windows.Foundation.Rect ToFoundationRect()
        {
            return new Windows.Foundation.Rect(
                _x, _y, _width, _height);
        }

        public override string ToString()
        {
            var separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;

            return string.Format("{1:}{0}{2:}{0}{3:}{0}{4:}",
                                 separator,
                                 _x,
                                 _y,
                                 _width,
                                 _height);
        }

        public bool Equals(Rect value)
        {
            return (this == value);
        }

        public static bool operator ==(Rect rect1, Rect rect2)
        {
            return rect1.X == rect2.X &&
                   rect1.Y == rect2.Y &&
                   rect1.Width == rect2.Width &&
                   rect1.Height == rect2.Height;
        }

        public static bool operator !=(Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }

        public override bool Equals(object o)
        {
            return o is Rect && this == (Rect)o;
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return X.GetHashCode() ^
                   Y.GetHashCode() ^
                   Width.GetHashCode() ^
                   Height.GetHashCode();
        }
    }
}