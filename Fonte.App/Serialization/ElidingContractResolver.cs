﻿// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System.Collections;
using System.Reflection;


namespace Fonte.App.Serialization
{
    public class ElidingContractResolver : DefaultContractResolver
    {
        public static readonly ElidingContractResolver Instance = new ElidingContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType == typeof(string))
            {
                // Do not include empty strings
                property.ShouldSerialize = instance =>
                {
                    return !string.IsNullOrWhiteSpace(instance.GetType().GetProperty(member.Name).GetValue(instance, null) as string);
                };
            }
            /*else if (property.PropertyType == typeof(DateTime))
            {
                // Do not include zero DateTime
                property.ShouldSerialize = instance =>
                {
                    return Convert.ToDateTime(instance.GetType().GetProperty(member.Name).GetValue(instance, null)) != default;
                };
            }*/
            else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                // Do not include zero-length lists
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        property.ShouldSerialize = instance =>
                        {
                            var enumerable = instance.GetType().GetProperty(member.Name).GetValue(instance, null) as IEnumerable;
                            return enumerable != null ? enumerable.GetEnumerator().MoveNext() : false;
                        };
                        break;
                    case MemberTypes.Field:
                        property.ShouldSerialize = instance =>
                        {
                            var enumerable = instance.GetType().GetField(member.Name).GetValue(instance) as IEnumerable;
                            return enumerable != null ? enumerable.GetEnumerator().MoveNext() : false;
                        };
                        break;
                }
            }
            return property;
        }
    }
}
