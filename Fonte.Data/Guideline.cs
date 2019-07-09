/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Newtonsoft.Json;

    using System;
    using System.Numerics;

    public partial class Guideline : ILayerElement, ILocatable
    {
        internal float _x;
        internal float _y;
        internal float _angle;
        internal string _name;

        internal bool _isSelected;

        // XXX serialize to writesingle ; check that it's needed
        [JsonProperty("x")]
        public float X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    new GuidelineXChange(this, value).Apply();
                }
            }
        }

        // XXX serialize to writesingle
        [JsonProperty("y")]
        public float Y
        {
            get => _y;
            set
            {
                if (value != _y)
                {
                    new GuidelineYChange(this, value).Apply();
                }
            }
        }

        [JsonProperty("angle")]
        public float Angle
        {
            get => _angle;
            set
            {
                if (value != _angle)
                {
                    new GuidelineAngleChange(this, value).Apply();
                }
            }
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    new GuidelineNameChange(this, value).Apply();
                }
            }
        }

        /**/

        [JsonIgnore]
        public Vector2 Direction
        {
            get
            {
                var rad = Conversion.ToRadians(Angle);
                return new Vector2(
                    (float)Math.Cos(rad),
                    (float)Math.Sin(rad)
                );
            }
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    new GuidelineIsSelectedChange(this, value).Apply();
                }
            }
        }

        [JsonIgnore]
        public object Parent { get; internal set; }

        [JsonConstructor]
        public Guideline(float x, float y, float angle, string name = null)
        {
            _x = x;
            _y = y;
            _angle = angle;
            _name = name ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{nameof(Guideline)}({X}, {Y}, angle: {Angle})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
}
