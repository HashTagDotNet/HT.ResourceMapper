using System.Text.Json;

namespace HT.Collections
{


    public class Property : Comparer<Property>, IComparable<Property>, IComparable, IEqualityComparer<Property>, IEquatable<Property>, IComparer<Property>, ICloneable
    {

        public Property()
        {
        }
        public Property(string key, object value)
        {
            Key = key;
            SetValue(value);
        }

        public Property(Property property)
        {
            this.Value = property.Value;
            this.ValueType = property.ValueType;
            this.Key = property.Key;
        }

        public Property(string key, string value)
        {
            Key = key;
            Value = value;
            ValueType = "System.String";
        }


        public string ValueType { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public Property SetValue(object value)
        {
            if (value == null)
            {
                Value = "(null)";
                ValueType = "(unknown)";
                return this;
            }
            else
            {
                if (value == null)
                {
                    Value = "(null)";
                    ValueType = "(undefined)";
                    return this;
                }

                Type t = value.GetType();
                ValueType = t.FullName;
                if (t.IsPrimitive)
                {
                    Value = value.ToString();
                }
                else
                {
                    switch (ValueType)
                    {
                        case "System.String":
                            Value = (string)value;
                            break;
                        case "System.DateTime":
                            Value = string.Format("{0:yyyy-DD-MMTHH:mm:ss.fff}", value);
                            break;
                        case "System.Guid":
                            Value = ((Guid)value).ToString();
                            break;
                        default:
                            ValueType = string.Format("{0}::json", t.FullName);
                            Value = JsonSerializer.Serialize(value);
                            break;
                    }
                }
                return this;
            }
        }

        public override bool Equals(object obj)
        {
            Property source = obj as Property;
            if (source == null)
            {
                return false;
            }
            return (string.Compare(source.Key, this.Key, StringComparison.InvariantCultureIgnoreCase) == 0 && string.Compare(source.Value, this.Value, StringComparison.InvariantCultureIgnoreCase) == 0);
        }
        public override int GetHashCode()
        {
            return GetHashCode(this);
        }
        public override string ToString()
        {
            return string.Format("[{0}]={1} ({2})",
                Key,
                Value,
                ValueType);
        }

        public override int Compare(Property left, Property right)
        {
            if (left != null && right == null) return 1;
            if (left == null && right == null) return 0;
            if (left == null && right != null) return -1;

            int result = string.Compare(left.Key, right.Key, StringComparison.InvariantCultureIgnoreCase);
            if (result != 0) return result;
            return string.Compare(left.Value, right.Value, StringComparison.InvariantCultureIgnoreCase);
        }

        public int CompareTo(Property other)
        {
            return Compare(this, other);
        }

        public int CompareTo(object obj)
        {
            return Compare(this, (Property)obj);
        }

        public bool Equals(Property x, Property y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(Property obj)
        {
            return Key.GetHashCode() ^ Value.GetHashCode();
        }

        public bool Equals(Property other)
        {
            return Equals(this, other);
        }

        public object Clone()
        {
            return (object)new Property()
            {
                Key = this.Key,
                Value = this.Value,
                ValueType = this.ValueType
            };
        }
    }
}