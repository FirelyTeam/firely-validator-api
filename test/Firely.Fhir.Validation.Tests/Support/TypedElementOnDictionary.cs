/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Language;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    internal class TypedElementOnDictionary : IDictionary<string, object>, ITypedElement, IResourceTypeSupplier, IAnnotated
    {
        private readonly IDictionary<string, object> _wrapped;
        private readonly string _name;
        private readonly string _location;

        public static ITypedElement ForObject(string name, object value) => forObject(name, value, name);

        private static ITypedElement forObject(string name, object value, string location)
        {
            if (value is ITypedElement ite) return ite;

            if (value is IDictionary<string, object> dict)
                return new TypedElementOnDictionary(name, dict, location);
            else
            {
                var ts = TypeSpecifier.ForNativeType(value.GetType());
                if (ts.Namespace == TypeSpecifier.DOTNET_NAMESPACE)
                {
                    var t = value.GetType().GetProperties();
                    var contents = t.Select(p => KeyValuePair.Create(p.Name, p.GetValue(value)));
                    contents = contents.Where(kvp => kvp.Value is not null);
                    return new TypedElementOnDictionary(name, new Dictionary<string, object>(contents!), location);
                }
                else
                {
                    _ = ElementNode.TryConvertToElementValue(value, out var primitive);
                    return new ConstantElement(name, ts.FullName, primitive, location);
                }
            }
        }
        private TypedElementOnDictionary(string rootName, IDictionary<string, object> wrapped, string location)
            => (_wrapped, _name, _location) = (wrapped, rootName, location);

        public TypedElementOnDictionary(string rootName, IDictionary<string, object> wrapped) : this(rootName, wrapped, rootName) { }

        public string Name => _name;

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public string? InstanceType => TryGetValue("_type", out var type) ? type as string : ResourceType;
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public object? Value => TryGetValue("_value", out var value) ? value : null;
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public string Location => _location;

        public IElementDefinitionSummary? Definition => null;

        public IEnumerable<ITypedElement> Children(string? name = null)
        {
            IEnumerable<KeyValuePair<string, object>> children;

            children = name switch
            {
                not null =>
                    _wrapped.TryGetValue(name, out var value) && value is not null ?
                        new KeyValuePair<string, object>[] { KeyValuePair.Create(name, value) }
                        : Enumerable.Empty<KeyValuePair<string, object>>(),
                _ => _wrapped
            };

            foreach (var child in children)
            {
                if (child.Value is IEnumerable ie && (child.Value is not string))
                {
                    int index = 0;
                    foreach (var childValue in ie)
                        if (childValue is not null) yield return forObject(child.Key, childValue, $"{_location}.{child.Key}[{index++}]");
                }
                else
                    yield return forObject(child.Key, child.Value, $"{_location}.{child.Key}");
            }
        }

        private record ConstantElement(string Name, string InstanceType, object Value, string Location) : ITypedElement
        {
            public IElementDefinitionSummary? Definition => null;

            public IEnumerable<ITypedElement> Children(string? name) =>
                Enumerable.Empty<ITypedElement>();
        }

        public IEnumerable<object>? Annotations(Type type) => type == typeof(IResourceTypeSupplier) ? (new[] { this }) : null;

        public string? ResourceType => TryGetValue("resourceType", out var rt) ? rt as string : null;

        #region "IDictionary"
        public object this[string key] { get => _wrapped[key]; set => _wrapped[key] = value; }

        public ICollection<string> Keys => _wrapped.Keys;

        public ICollection<object> Values => _wrapped.Values;

        public int Count => _wrapped.Count;

        public bool IsReadOnly => _wrapped.IsReadOnly;

        public void Add(string key, object value) => _wrapped.Add(key, value);
        public void Add(KeyValuePair<string, object> item) => _wrapped.Add(item);
        public void Clear() => _wrapped.Clear();
        public bool Contains(KeyValuePair<string, object> item) => _wrapped.Contains(item);
        public bool ContainsKey(string key) => _wrapped.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _wrapped.CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _wrapped.GetEnumerator();
        public bool Remove(string key) => _wrapped.Remove(key);
        public bool Remove(KeyValuePair<string, object> item) => _wrapped.Remove(item);
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _wrapped.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_wrapped).GetEnumerator();
        #endregion
    }

    internal static class DictionaryElementExtensions
    {
        public static ITypedElement ToTypedElement(this object node, string name = "root") =>
            TypedElementOnDictionary.ForObject(name, node);
    }

}