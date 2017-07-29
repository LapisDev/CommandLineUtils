using System;
using System.Linq;
using System.Reflection;

namespace Lapis.CommandLineUtils.Converters
{
    public class MethodConverter : IConverter
    {
        public bool CanConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
                return false;
            if (sourceType == null)
                return false;
            return (GetStaticToMethod(sourceType, targetType) ??
                GetStaticFromMethod(sourceType, targetType) ??
                GetInstanceToMethod(sourceType, targetType)) != null;
        }

        public object Convert(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var staticMethod = GetStaticToMethod(value.GetType(), targetType) ??
                GetStaticFromMethod(value.GetType(), targetType);
            if  (staticMethod != null)
                return staticMethod.Invoke(null, new [] { value });

            var instanceMethod = GetStaticFromMethod(value.GetType(), targetType);
            if (instanceMethod != null)
                return instanceMethod.Invoke(value, null);
            
            throw new InvalidCastException();
        }

        private MethodInfo GetStaticToMethod(Type sourceType, Type targetType)
        {           
            var candidates = sourceType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => !m.IsAbstract)
                .Where(m => targetType.IsAssignableFrom(m.ReturnType))
                .Where(m => 
                {
                    var parameters = m.GetParameters();
                    return parameters.Length == 1 && 
                        parameters[0].ParameterType == sourceType &&
                        parameters[0].IsIn;
                })
                .ToList();
            
            var opImplicit = candidates.Where(m => m.Name == "op_Implicit").FirstOrDefault();
            if (opImplicit != null)
                return opImplicit;
            
            var opExplicit = candidates.Where(m => m.Name == "op_Explicit").FirstOrDefault();
            if (opExplicit != null)
                return opExplicit;

            if (targetType == typeof(bool))
            {
                var opTrue = candidates.Where(m => m.Name == "op_True").FirstOrDefault();
                if (opTrue != null)
                    return opTrue;
            }

            var toType = candidates.Where(m => m.Name == $"To{targetType.Name}").FirstOrDefault();
            if (toType != null)
                return toType;

            return null;
        }

        private MethodInfo GetStaticFromMethod(Type sourceType, Type targetType)
        {           
            var candidates = targetType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => !m.IsAbstract)
                .Where(m => m.ReturnType == targetType)
                .Where(m => 
                {
                    var parameters = m.GetParameters();
                    return parameters.Length == 1 && 
                        parameters[0].ParameterType.IsAssignableFrom(sourceType) &&
                        parameters[0].IsIn;
                })
                .ToList();
            
            var opImplicit = candidates.Where(m => m.Name == "op_Implicit").FirstOrDefault();
            if (opImplicit != null)
                return opImplicit;
            
            var opExplicit = candidates.Where(m => m.Name == "op_Explicit").FirstOrDefault();
            if (opExplicit != null)
                return opExplicit;

            if (targetType == typeof(bool))
            {
                var opTrue = candidates.Where(m => m.Name == "op_True").FirstOrDefault();
                if (opTrue != null)
                    return opTrue;
            }

            var fromType = candidates.Where(m => m.Name == $"From{sourceType.Name}").FirstOrDefault();
            if (fromType != null)
                return fromType;

            var parseType = candidates.Where(m => m.Name == $"Parse").FirstOrDefault();
            if (parseType != null)
                return parseType;

            return null;
        }

        private MethodInfo GetInstanceToMethod(Type sourceType, Type targetType)
        {           
            var candidates = sourceType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => !m.IsAbstract)
                .Where(m => targetType.IsAssignableFrom(m.ReturnType))
                .Where(m => m.GetParameters().Length == 0)
                .ToList();            

            var toType = candidates.Where(m => m.Name == $"To{targetType.Name}").FirstOrDefault();
            if (toType != null)
                return toType;

            return null;
        }

    }
}