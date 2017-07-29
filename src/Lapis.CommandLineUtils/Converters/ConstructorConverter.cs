using System;
using System.Linq;
using System.Reflection;

namespace Lapis.CommandLineUtils.Converters
{
    public class ConstrctorConverter : IConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
                return false;
            if (sourceType == null)
                return false;
            return GetConstrctor(sourceType, targetType) != null;
        }

        public object Convert(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var constrctor = GetConstrctor(value.GetType(), targetType);
            if (constrctor != null)
                return constrctor.Invoke(new [] { value });
            
            throw new InvalidCastException();
        }

        private ConstructorInfo GetConstrctor(Type sourceType, Type targetType)
        {           
            var constructor = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)  
                .Where(m => !m.IsAbstract)              
                .Where(m => 
                {
                    var parameters = m.GetParameters();
                    return parameters.Length == 1 && 
                        parameters[0].ParameterType.IsAssignableFrom(sourceType);
                })
                .FirstOrDefault();
            
            if (constructor != null)
                return constructor;

            return null;
        }
    }
}