/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Tests
{
    internal class ElementNodeAdapter : ITypedElement
    {
        private readonly ElementNode _elementNodeInstance;
        private readonly IStructureDefinitionSummaryProvider _structureDefinitionSummaryProvider;

        public string Name => _elementNodeInstance.Name;

        public string InstanceType => _elementNodeInstance.InstanceType;

        public object Value => _elementNodeInstance.Value;

        public string Location => _elementNodeInstance.Location;

        public IElementDefinitionSummary Definition => _elementNodeInstance.Definition;

        public static ElementNodeAdapter Root(string type, string? name = null, object? value = null)
        {
            var def = new NoTypeProvider();
            var instance = ElementNode.Root(def, type, name, value);
            return new ElementNodeAdapter(instance, def);

        }
        private ElementNodeAdapter(ElementNode elementNode, IStructureDefinitionSummaryProvider structureDefinitionSummaryProvider)
        {
            _elementNodeInstance = elementNode;
            _structureDefinitionSummaryProvider = structureDefinitionSummaryProvider;
        }

        public IEnumerable<ITypedElement> Children(string? name = null) => _elementNodeInstance.Children(name);

        internal void Add(string name, object? value = null, string? instanceType = null)
            => _elementNodeInstance.Add(_structureDefinitionSummaryProvider, name, value, instanceType);


        internal void Add(ITypedElement child, string name)
        {
            switch (child)
            {
                case ElementNodeAdapter node:
                    _elementNodeInstance.Add(_structureDefinitionSummaryProvider, node._elementNodeInstance, name);
                    break;
                default:
                    throw new ArgumentException($"{nameof(child)} is not of type {nameof(ElementNodeAdapter)}");
            }
        }

        private class NoTypeProvider : IStructureDefinitionSummaryProvider
        {
            public IStructureDefinitionSummary? Provide(string canonical) => null;
        }
    }

    internal static class ElementNodeAdapterExtensions
    {
        public static ITypedElement CreateHumanName(string familyName, string[] givenNames)
        {
            var node = ElementNodeAdapter.Root("HumanName");
            if (!string.IsNullOrEmpty(familyName))
                node.Add("family", familyName, "string");
            foreach (var givenName in givenNames)
                node.Add("given", givenName, "string");
            return node;
        }

        public static ITypedElement CreateCoding(string code, string system, bool systemFirstInOrder)
        {
            var node = ElementNodeAdapter.Root("Coding");
            if (systemFirstInOrder)
            {
                node.Add("system", system, "string");
                node.Add("code", code, "string");
            }
            else
            {
                node.Add("code", code, "string");
                node.Add("system", system, "string");
            }

            return node;
        }
    }
}
