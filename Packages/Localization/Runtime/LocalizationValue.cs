using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Localizations
{
    public struct LocalizationValue : IEquatable<LocalizationValue>
    {
        public LocalizationValue(string typeName, object value)
        {
            this.Value = value;
            this.TypeName = typeName;
            this.StringValue = value as string;
        }

        public LocalizationValue(string value)
        {
            this.TypeName = "string";
            this.Value = value;
            this.StringValue = value;
        }

        public object Value { get; set; }

        public string StringValue { get; set; }

        public string TypeName { get; set; }



        public static Dictionary<string, LocalizationValue> StringDictionary(IDictionary<string, string> dic)
        {
            Dictionary<string, LocalizationValue> result = new Dictionary<string, LocalizationValue>();
            foreach (var item in dic)
            {
                result[item.Key] = new LocalizationValue(item.Value);
            }
            return result;
        }

        public LocalizationValue Clone()
        {
            return new LocalizationValue() { TypeName = TypeName, Value = Value, StringValue = StringValue };
        }


        public bool Equals(LocalizationValue other)
        {
            return StringValue == other.StringValue &&
                object.Equals(Value, other.Value) &&
                TypeName == other.TypeName;
        }

        public override bool Equals(object obj)
        {
            return obj is LocalizationValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StringValue, Value, TypeName);
        }

        public override string ToString()
        {
            if (Value == null)
                return string.Empty;
            return Value.ToString();
        }
        public static bool operator ==(LocalizationValue a, LocalizationValue b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(LocalizationValue a, LocalizationValue b)
        {
            return !a.Equals(b);
        }
    }




}