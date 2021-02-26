/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Firely.Fhir.Validation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Validation.Compilation
{
    internal static class SchemaConverterExtensions
    {
        public static IElementSchema Convert(this ElementDefinition def)
        {

            var elements = new List<IAssertion>()
                .maybeAdd(def, buildMaxLength)
                .maybeAdd(buildFixed(def))
                .maybeAdd(buildPattern(def))
                .maybeAdd(buildBinding(def))
                .maybeAdd(buildMinValue(def))
                .maybeAdd(buildMaxValue(def))
                .maybeAdd(buildFp(def))
                .maybeAdd(buildCardinality(def))
                .maybeAdd(buildElementRegEx(def))
                .maybeAdd(buildTypeRefRegEx(def))
                .maybeAdd(BuildTypeRefValidation(def))
               ;

            return new ElementSchema(id: new Uri("#" + def.Path, UriKind.Relative), elements);
        }

        public static IAssertion ValueSlicingConditions(this ElementDefinition def)
        {
            var elements = new List<IAssertion>()
                   .maybeAdd(buildFixed(def))
                   .maybeAdd(buildPattern(def))
                   .maybeAdd(buildBinding(def));

            return new AllAssertion(elements);
        }

        private static IAssertion? buildBinding(ElementDefinition def)
            => def.Binding is not null ? new BindingAssertion(def.Binding.ValueSet, convertStrength(def.Binding.Strength), false, def.Binding.Description) : null;

        private static BindingAssertion.BindingStrength? convertStrength(BindingStrength? strength) => strength switch
        {
            BindingStrength.Required => BindingAssertion.BindingStrength.Required,
            BindingStrength.Extensible => BindingAssertion.BindingStrength.Extensible,
            BindingStrength.Preferred => BindingAssertion.BindingStrength.Preferred,
            BindingStrength.Example => BindingAssertion.BindingStrength.Example,
            _ => default,
        };

        // Adds a regex for the value for each of the typerefs in the ElementDef.Type if it has a "regex" extension on it.
        private static IAssertion? buildTypeRefRegEx(ElementDefinition def)
        {
            var list = new List<IAssertion>();

            foreach (var type in def.Type)
            {
                list.maybeAdd(buildRegex(type));
            }
            return list.Count > 0 ? new ElementSchema(id: new Uri("#" + def.Path, UriKind.Relative), list) : null;
        }

        // Adds a regex for the value if the ElementDef has a "regex" extension on it.
        private static IAssertion? buildElementRegEx(ElementDefinition def) =>
            buildRegex(def);

        private static IAssertion? buildMinValue(ElementDefinition def) =>
            def.MinValue != null ? new MinMaxValue(def.MinValue.ToTypedElement(), MinMax.MinValue) : null;

        private static IAssertion? buildMaxValue(ElementDefinition def) =>
            def.MaxValue != null ? new MinMaxValue(def.MaxValue.ToTypedElement(), MinMax.MaxValue) : null;

        private static IAssertion? buildFixed(ElementDefinition def) =>
            def.Fixed != null ? new Fixed(def.Fixed.ToTypedElement()) : null;

        private static IAssertion? buildPattern(ElementDefinition def) =>
           def.Pattern != null ? new Pattern(def.Pattern.ToTypedElement()) : null;

        private static IAssertion? buildMaxLength(ElementDefinition def) =>
            def.MaxLength.HasValue ? new MaxLength(def.MaxLength.Value) : null;

        private static IAssertion? buildFp(ElementDefinition def)
        {
            var list = new List<IAssertion>();
            foreach (var constraint in def.Constraint)
            {
                var bestPractice = constraint.GetBoolExtension("http://hl7.org/fhir/StructureDefinition/elementdefinition-bestpractice") ?? false;
                var fpAssertion = new FhirPathAssertion(constraint.Key, constraint.Expression, constraint.Human, convertConstraintSeverity(constraint.Severity), bestPractice);
                list.Add(fpAssertion);
            }

            return list.Any() ? new ElementSchema(id: new Uri("#constraints", UriKind.Relative), list) : null;

            static IssueSeverity? convertConstraintSeverity(ElementDefinition.ConstraintSeverity? constraintSeverity) => constraintSeverity switch
            {
                ElementDefinition.ConstraintSeverity.Error => IssueSeverity.Error,
                ElementDefinition.ConstraintSeverity.Warning => IssueSeverity.Warning,
                _ => default,
            };
        }

        private static IAssertion? buildCardinality(ElementDefinition def) =>
            def.Min != null || def.Max != null ? new CardinalityAssertion(def.Min, def.Max, def.Path) : null;

        private static IAssertion? buildRegex(IExtendable elementDef)
        {
            var pattern = elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/regex");
            return pattern != null ? new RegExAssertion(pattern) : null;
        }

        // TODO this should be somewhere else
        private static AggregationMode convertAggregationMode(ElementDefinition.AggregationMode aggregationMode)
            => aggregationMode switch
            {
                ElementDefinition.AggregationMode.Bundled => AggregationMode.Bundled,
                ElementDefinition.AggregationMode.Contained => AggregationMode.Contained,
                ElementDefinition.AggregationMode.Referenced => AggregationMode.Referenced,
                _ => throw new InvalidOperationException("ElementDefinition.AggregationMode and AggregationMode are not in sync anymore.")
            };


        public static IAssertion? BuildTypeRefValidation(this ElementDefinition def) =>
            TypeReferenceConverter.ConvertTypeReferences(def.Type, def.Path);

        private static List<IAssertion> maybeAdd(this List<IAssertion> assertions, IAssertion? element)
        {
            if (element is not null)
                assertions.Add(element);

            return assertions;
        }

        private static List<IAssertion> maybeAdd(this List<IAssertion> assertions, ElementDefinition def, Func<ElementDefinition, IAssertion?> builder)
        {
            // TODOL handle "compile" exceptions
            IAssertion? element = null;
            try
            {
                element = builder(def);
            }
            catch (ArgumentNullException)
            {
            }
            catch (IncorrectElementDefinitionException ex)
            {
                element = new CompileAssertion(ex.Message);
            }
            catch (Exception)
            {

                throw;
            }
            return assertions.maybeAdd(element!);
        }
    }

    /// <summary>
    /// This should be moved to project Firely.Fhir.Validation. Or we should use another construction to
    /// inform the user about schema compilation errors
    /// </summary>
    internal class CompileAssertion : IAssertion
    {
        public readonly string? Message;

        public CompileAssertion(string? message)
        {
            Message = message;
        }

        public JToken ToJson()
        {
            throw new NotImplementedException();
        }
    }
}
