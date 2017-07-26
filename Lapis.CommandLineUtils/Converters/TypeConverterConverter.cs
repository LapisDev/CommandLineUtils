using System;
using System.ComponentModel;

namespace Lapis.CommandLineUtils.Converters
{
    public class TypeConverterConverter : IConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
                return false;
            if (sourceType == null)
                return false;
            
            return (TypeDescriptor.GetConverter(sourceType)?.CanConvertTo(targetType) ?? false) ||
                (TypeDescriptor.GetConverter(targetType)?.CanConvertFrom(sourceType) ?? false);
        }

        public object Convert(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var converter = TypeDescriptor.GetConverter(value.GetType());
            if (converter?.CanConvertTo(targetType) ?? false)
                return converter.ConvertTo(value, targetType);
            converter = TypeDescriptor.GetConverter(targetType);
            if (converter?.CanConvertFrom(value.GetType()) ?? false)
                return converter.ConvertFrom(value);
            throw new InvalidCastException();
        }
    }
}