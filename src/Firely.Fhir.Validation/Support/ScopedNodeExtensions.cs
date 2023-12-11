using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Helper methods to convert between <see cref="IScopedNode"/> and <see cref="IReadOnlyDictionary{TKey, TValue}"/>
    /// </summary>
    internal static class ScopedNodeExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="inspector"></param>
        /// <returns></returns>
        public static IScopedNode ToScopedNode(this IReadOnlyDictionary<string, object> node, ModelInspector? inspector = null)
        {
            inspector ??= ModelInspector.ForAssembly(node.GetType().Assembly);
            return new ScopedNodeOnDictionary(inspector, node.GetType().Name, node);
        }
    }

    internal class ScopedNodeOnDictionary : IScopedNode
    {
        private readonly IReadOnlyDictionary<string, object> _wrapped;
        private readonly IScopedNode? _parentNode;
        private readonly ModelInspector _inspector;
        private readonly ClassMapping? _myClassMapping;
        private readonly string _name;

        public ScopedNodeOnDictionary(ModelInspector inspector, string rootName, IReadOnlyDictionary<string, object> wrapped, IScopedNode? parentNode = null)
        {
            (_wrapped, _parentNode, _inspector, _name) = (wrapped, parentNode, inspector, rootName);
            _myClassMapping = _inspector.FindOrImportClassMapping(_wrapped.GetType());
            InstanceType = (_myClassMapping as IStructureDefinitionSummary)?.TypeName;
        }

        public IScopedNode? Parent => _parentNode;

        public string Name => _name;

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public string? InstanceType { get; private set; }

        public object? Value => _wrapped.TryGetValue("value", out var value) && isNETPrimitiveType(value) ? value : null;
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public IEnumerable<IScopedNode> Children(string? name = null)
        {
            IEnumerable<KeyValuePair<string, object>> children = name switch
            {
                not null =>
                    _wrapped.TryGetValue(name, out var value) && value is not null ?
                        new KeyValuePair<string, object>[] { KeyValuePair.Create(name, value) }
                        : Enumerable.Empty<KeyValuePair<string, object>>(),
                _ => _wrapped
            };

            foreach (var child in children)
            {
                if (child.Value is IList coll)
                {
                    foreach (var childValue in coll)
                        if (childValue is IReadOnlyDictionary<string, object> dict)
                            yield return new ScopedNodeOnDictionary(_inspector, child.Key, dict, this);
                }
                else if (child.Value is IReadOnlyDictionary<string, object> re)
                    yield return new ScopedNodeOnDictionary(_inspector, child.Key, re, this);
                else if (child.Key != "value" && isNETPrimitiveType(child.Value))
                    yield return new ConstantElement(child.Key, child.Value.GetType().Name, child.Value, this);

            }
        }

        private bool isNETPrimitiveType(object a)
            => a is string or bool or decimal or DateTimeOffset or int or long or byte[] or XHtml;

        internal record ConstantElement(string Name, string InstanceType, object Value, IScopedNode Parent) : IScopedNode
        {
            public IEnumerable<IScopedNode> Children(string? name) =>
                Enumerable.Empty<IScopedNode>();
        }
    }
}