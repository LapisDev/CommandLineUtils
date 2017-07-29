using System;

namespace Lapis.CommandLineUtils.Converters
{
    public class SystemConvertConverter : IConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
                return false;
            if (sourceType == null)
                return false;
            if (!typeof(IConvertible).IsAssignableFrom(sourceType))
                return false;
            return true;
        }

        public object Convert(object value, Type targetType)
        {
            return System.Convert.ChangeType(value, targetType);
        }
    }
}