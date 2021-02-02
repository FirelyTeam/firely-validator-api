﻿using Hl7.Fhir.ElementModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public class ValidationElementDefinitionAssertionFactory : IElementDefinitionAssertionFactory
    {
        public IAssertion CreateBindingAssertion(string valueSetUri, BindingAssertion.BindingStrength? strength, bool abstractAllowed = true, string? description = null)
            => new BindingAssertion(valueSetUri, strength, abstractAllowed, description);

        public IAssertion CreateCardinalityAssertion(int? min, string max, string? location = null)
            => new CardinalityAssertion(min, max, location);

        public IAssertion CreateChildren(Func<IReadOnlyDictionary<string, IAssertion>> childGenerator, bool allowAdditionalChildren = true)
            => new Children(childGenerator, allowAdditionalChildren);

        public IAssertion? CreateConditionsAssertion()
            => null; // todo

        public void CreateDefaultValue(ITypedElement defaultValue)
        { }

        public void CreateDocumentation(string label, string shortDescription, string definition, string comment, string requirements, string meaningWhenMissing, string orderMeaning, IEnumerable<string> aliases, IEnumerable<(string system, string systemVersion, string code, string codeDisplay, bool isUserSelected)> codings)
        { }

        public IElementSchema CreateElementSchemaAssertion(Uri id, IEnumerable<IAssertion>? assertions = null)
            => new ElementSchema(id, assertions);

        public void CreateExampleValues(IEnumerable<(string label, ITypedElement value)> exampleValues)
        { }

        public IAssertion CreateFhirPathAssertion(string key, string expression, string humanDescription, IssueSeverity? severity, bool bestPractice)
            => new FhirPathAssertion(key, expression, humanDescription, severity, bestPractice);

        public IAssertion CreateFixedValueAssertion(ITypedElement fixedValue)
            => new Fixed(fixedValue);

        public IAssertion? CreateIsModifierAssertion(bool isModifier, string? reason = null)
            => null; // todo

        public IAssertion CreateMaxLengthAssertion(int maxLength)
            => new MaxLength(maxLength);

        public IAssertion CreateMinMaxValueAssertion(ITypedElement minMaxValue, MinMax minMaxType)
            => new MinMaxValue(minMaxValue, minMaxType);

        public IAssertion? CreateMustSupportAssertion(bool mustSupport)
            => null;

        public IAssertion CreatePatternAssertion(ITypedElement patternValue)
            => new Pattern(patternValue);

        public IAssertion CreateReferenceAssertion(Func<Uri?, Task<IElementSchema>> getSchema, Uri? uri, IEnumerable<AggregationMode?>? aggregations = null)
            => new ReferenceAssertion(getSchema, uri, aggregations);

        public IAssertion CreateExtensionAssertion(Func<Uri?, Task<IElementSchema>> getSchema, Uri uri)
            => new ExtensionAssertion(getSchema, uri);

        public IAssertion CreateRegexAssertion(string pattern)
            => new RegExAssertion(pattern);

        public IAssertion? CreateTypesAssertion(IEnumerable<(string code, IEnumerable<string> profileCanonicals)> types)
            => null; // todo

        public IAssertion CreateSliceAssertion(bool ordered, IAssertion @default, IEnumerable<SliceAssertion.Slice> slices)
            => new SliceAssertion(ordered, @default, slices);

        public SliceAssertion.Slice CreateSlice(string name, IAssertion condition, IAssertion assertion)
            => new SliceAssertion.Slice(name, condition, assertion);
    }
}
