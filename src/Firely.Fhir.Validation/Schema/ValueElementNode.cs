/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Language;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Firely.Fhir.Validation
{
    internal class ValueElementNode : ITypedElement
    {
        private readonly ITypedElement _wrapped;

        public ValueElementNode(ITypedElement wrapped)
        {
            _wrapped = wrapped;
        }


        public IEnumerable<ITypedElement> Children(string? name = null) => [];

        IEnumerable<ITypedElement> ITypedElement.Children(string? name) => Children(name);

        public string Name => "value";

        public string? InstanceType => (_wrapped.Value is not null) ? TypeSpecifier.ForNativeType(_wrapped.Value.GetType()).FullName : null;

        public object? Value => _wrapped.Value;
        
        public string Location => _wrapped.Location;
        
        public IElementDefinitionSummary? Definition { get; } 
    }
}
