using System;
using System.Linq;
using System.Reflection;

namespace Lapis.CommandLineUtils.Converters
{
    public class EnumNameConverter : IConverter
    {
        public bool IgnoreCase { get; set; }

        public bool CanConvert(Type sourceType, Type targetType)
        {   
            if (targetType == null)
                return false;
            if (sourceType == null)
                return false;
            return (targetType.IsEnum && sourceType == typeof(string)) ||
                (targetType == typeof(string) && sourceType.IsEnum);
        }

        public object Convert(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (targetType.IsEnum && value is string)
                return Enum.Parse(targetType, (string)value, IgnoreCase);
            
            if (targetType == typeof(string) && value.GetType().IsEnum)
                return Enum.GetName(targetType, value);

            throw new InvalidCastException();
        }
    }
}