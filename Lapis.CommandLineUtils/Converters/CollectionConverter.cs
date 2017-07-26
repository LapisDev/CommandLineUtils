using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lapis.CommandLineUtils.Converters
{
    public class CollectionConverter : IConverter
    {
        public CollectionConverter(IConverter itemConverter)
        {
            if (itemConverter == null)
                throw new ArgumentNullException(nameof(itemConverter));
            ItemConverter = itemConverter;
        }

        public IConverter ItemConverter { get; }

        public bool CanConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
                return false;
            if (sourceType == null)
                return false;

            Type sourceItemType;
            if (sourceType.IsArray && sourceType.GetArrayRank() == 1)
                sourceItemType = sourceType.GetElementType();
            else if (typeof(IEnumerable<>).IsAssignableFrom(sourceType) &&
                sourceType.IsGenericType &&
                !sourceType.ContainsGenericParameters &&
                sourceType.GenericTypeArguments.Length == 1)
                sourceItemType = sourceType.GenericTypeArguments[0];
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(sourceType))
                sourceItemType = typeof(object);
            else
                return false;

            Type targetItemType;
            if (targetType.IsArray && targetType.GetArrayRank() == 1)
                targetItemType = sourceType.GetElementType();
            else if (targetType.IsGenericType &&
                !targetType.ContainsGenericParameters &&
                targetType.GenericTypeArguments.Length == 1)
            {
                var targetCollectionType = targetType.GetGenericTypeDefinition();
                targetItemType = targetType.GenericTypeArguments[0];
                var constrctor = GetCollectionConstructor(targetCollectionType, targetItemType);
                if (constrctor == null)
                    return false;
            }
            else
                return false;

            if (!ItemConverter.CanConvert(sourceItemType, targetItemType))
                return false;
            return true;
        }

        public object Convert(object value, Type targetType)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var sourceType = value.GetType();
            Type sourceItemType;
            if (sourceType.IsArray && sourceType.GetArrayRank() == 1)
                sourceItemType = sourceType.GetElementType();
            else if (typeof(IEnumerable<>).IsAssignableFrom(sourceType) &&
                sourceType.IsGenericType &&
                !sourceType.ContainsGenericParameters &&
                sourceType.GenericTypeArguments.Length == 1)
                sourceItemType = sourceType.GenericTypeArguments[0];
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(sourceType))
                sourceItemType = typeof(object);
            else
                throw new InvalidCastException();

            if (targetType.IsArray && targetType.GetArrayRank() == 1)
            {
                var targetItemType = sourceType.GetElementType();
                var items = new List<object>();
                foreach (var item in value as System.Collections.IEnumerable)
                    items.Add(ItemConverter.Convert(item, targetItemType));
                var array = Array.CreateInstance(targetItemType, items.Count);
                for (var i = 0; i < items.Count; i++)
                    array.SetValue(items[i], i);
                return array;
            }
            else if (targetType.IsGenericType &&
                !targetType.ContainsGenericParameters &&
                targetType.GenericTypeArguments.Length == 1)
            {
                var targetCollectionType = targetType.GetGenericTypeDefinition();
                var targetItemType = targetType.GenericTypeArguments[0];
                var constrctor = GetCollectionConstructor(targetCollectionType, targetItemType);
                if (constrctor != null)
                {
                    var parameters = new List<object>();
                    foreach (var item in value as System.Collections.IEnumerable)
                        parameters.Add(ItemConverter.Convert(item, targetItemType));
                    return constrctor.Invoke(new[] { parameters });
                }
            }
            throw new InvalidCastException();
        }

        private ConstructorInfo GetCollectionConstructor(Type collectionType, Type itemType)
        {
            var genericConstructorArgumentType = typeof(IList<>).MakeGenericType(itemType);
            return GetCollectionConstructor(collectionType, itemType, genericConstructorArgumentType);
        }

        private ConstructorInfo GetCollectionConstructor(Type collectionType, Type itemType, Type constructorArgumentType)
        {
            var genericEnumerable = typeof(IEnumerable<>).MakeGenericType(itemType);
            var constructors = collectionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => !m.IsAbstract)
                .Where(constructor => constructor.GetParameters().Length == 1);
            return (
                constructors.Where(constructor => constructor.GetParameters()[0].ParameterType == genericEnumerable)
                    .FirstOrDefault() ??
                constructors.Where(constructor => constructor.GetParameters()[0].ParameterType.IsAssignableFrom(constructorArgumentType))
                    .LastOrDefault()
            );
        }
    }
}