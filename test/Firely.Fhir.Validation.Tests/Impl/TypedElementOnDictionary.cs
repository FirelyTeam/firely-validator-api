using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Language;
using Hl7.Fhir.Specification;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Firely.Fhir.Validation.Tests.Impl
{
    internal class TypedElementOnDictionary : IDictionary<string, object>, ITypedElement
    {
        private readonly IDictionary<string, object> _wrapped;
        private readonly string _name;

        public static ITypedElement ForObject(string name, object value)
        {
            if (value is IDictionary<string, object> dict)
                return new TypedElementOnDictionary(name, dict);
            else
            {
                var ts = TypeSpecifier.ForNativeType(value.GetType());
                if (ts.Namespace == TypeSpecifier.DOTNET_NAMESPACE)
                {
                    var t = value.GetType().GetProperties();
                    var contents = t.Select(p => KeyValuePair.Create(p.Name, p.GetValue(value)));
                    contents = contents.Where(kvp => kvp.Value is not null);
                    return new TypedElementOnDictionary(name, new Dictionary<string, object>(contents!));
                }
                else
                {
                    _ = ElementNode.TryConvertToElementValue(value, out var primitive);
                    return new ConstantElement(name, ts.FullName, primitive);
                }
            }
        }

        public TypedElementOnDictionary(string rootName, IDictionary<string, object> wrapped) => (_wrapped, _name) = (wrapped, rootName);

        public string Name => _name;

        public string? InstanceType => TryGetValue("_type", out var type) ? type as string :
            TryGetValue("resourceType", out var rt) ? rt as string : null;

        public object? Value => TryGetValue("_value", out var value) ? value : null;

        public string Location => "none";

        public IElementDefinitionSummary? Definition => null;

        public IEnumerable<ITypedElement> Children(string? name = null)
        {
            IEnumerable<KeyValuePair<string, object>> children;

            if (name is not null)
            {
                children = _wrapped.TryGetValue(name, out var value) && value is not null ?
                    new KeyValuePair<string, object>[] { KeyValuePair.Create(name, value) }
                    : Enumerable.Empty<KeyValuePair<string, object>>();
            }
            else
                children = _wrapped;

            foreach (var child in children)
            {
                if (child.Value is IEnumerable ie && !(child.Value is string))
                {
                    foreach (var childValue in ie)
                        if (childValue is not null) yield return ForObject(child.Key, childValue);
                }
                else
                    yield return ForObject(child.Key, child.Value);
            }
        }

        record ConstantElement(string Name, string InstanceType, object Value) : ITypedElement
        {
            public string Location => "none";

            public IElementDefinitionSummary? Definition => null;

            public IEnumerable<ITypedElement> Children(string name) =>
                Enumerable.Empty<ITypedElement>();
        }


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


    static class DictionaryElementExtensions
    {
        public static ITypedElement ToTypedElement(this object node, string name = "root") =>
            TypedElementOnDictionary.ForObject(name, node);
    }

}
