/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

global using ROD = Firely.Fhir.Validation.IObjectTree;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    public interface IObjectTree : IReadOnlyDictionary<string, object>
    {
        public string Location { get; }

        /// <summary>
        /// The full canonical of the datatype this ROD represent
        /// </summary>
        public Canonical TypeCanonical { get; }

        // This really should not be necessary anymore, but unfortunately...
        // It's easy to implement in a ScopedRod, though
        public string Name { get; }
    }

    public static class RodExtensions
    {
        /// <summary>
        /// Port straight from the ScopedNode extension method Resolve()
        /// </summary>
        public static ScopedRod Resolve(this ScopedRod r, string reference) => throw new NotImplementedException();

        /// <summary>
        /// Simulate ITypedElement good enough for FhirPath to accept it.
        /// </summary>
        internal static ITypedElement ToTypedElement(this ScopedRod sr) => throw new NotImplementedException();

        /// <summary>
        /// Just like the old ITypedElement.Matches comparison extension method
        /// </summary>
        internal static bool Matches(this ROD l, ROD r) => throw new NotImplementedException();

        /// <summary>
        /// Wrapper class that simulates ROD on top of ITypedElement (CK already wrote it??)
        /// </summary>
        public static ROD ToRod(this ITypedElement te) => throw new NotImplementedException();

        /// <summary>
        /// Serialize it, reparse using Newtonsoft - this is not used in hotpaths anyway.
        /// </summary>
        internal static JObject ToJObject(this ROD r) => throw new NotImplementedException();

        /// <summary>
        /// Just serialize it.
        /// </summary>
        public static string ToJson(this ROD r) => throw new NotImplementedException();

        /// <summary>
        /// Just like the old ITypedElement.IsEqualTo comparison extension method
        /// </summary>
        public static bool IsEqualTo(this ROD l, ROD r) => throw new NotImplementedException();

        internal static ScopedRod AsScoped(this ROD r) => ScopedRod.Wrap(r);

        internal static ROD FromPrimitiveValue(object o) => o is ROD r ? r : new ValueRod(o);

        internal static object? GetValue(this ROD r) => r.TryGetValue("value", out var value) ? value : null;

        /// <summary>
        /// Whether the ROD represents something bindeable.
        /// </summary>
        /// <remarks>Having no instance type (is that even possible???) should return false
        /// </remarks>
        internal static bool IsBindeable(this ROD r) => throw new NotImplementedException();

        /// <summary>
        /// Parses the ROD to a code/Coding/CodeableConcept POCO if possible.
        /// </summary>
        internal static Element ParseToCommonBindeable(this ROD r) => throw new NotImplementedException();

        /// <summary>
        /// Given a canonical, would return the "local" name if it is a standard FHIR canonical, otherwise the full canonical.
        /// </summary>
        internal static string ShortTypeName(this ROD r) => throw new NotImplementedException();

        internal static IEnumerable<ROD> Children(this ROD r, string name) =>
            r.TryGetValue(name, out var child) switch
            {
                true when child is IEnumerable<ROD> children => children,
                true => Enumerable.Repeat(FromPrimitiveValue(child), 1),
                false => Enumerable.Empty<ROD>()
            };
    }


    public class ScopedRod : ROD
    {
        public static ScopedRod Wrap(ROD tree) =>
            tree is ScopedRod sr ? sr : new ScopedRod(tree);

        public ScopedRod(ROD wrapped)
        {
            Wrapped = wrapped;
        }

        public object this[string key] => throw new System.NotImplementedException();

        public ROD Wrapped { get; }

        public ScopedRod ResourceContext { get; }

        public IEnumerable<string> Keys => throw new System.NotImplementedException();

        public IEnumerable<object> Values => throw new System.NotImplementedException();

        public int Count => throw new System.NotImplementedException();

        public string Location => throw new NotImplementedException();

        public Canonical TypeCanonical => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public bool ContainsKey(string key) => throw new System.NotImplementedException();
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => throw new System.NotImplementedException();
        public bool TryGetValue(string key, out object value) => throw new System.NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();
    }
}