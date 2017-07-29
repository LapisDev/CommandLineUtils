using System;

namespace Lapis.CommandLineUtils.Converters
{
    public interface IConverter
    {
        object Convert(object value, Type targetType);

        bool CanConvert(Type sourceType, Type targetType);
    }
}