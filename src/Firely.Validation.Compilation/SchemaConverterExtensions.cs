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
using Hl7.Fhir.Validation;
using Hl7.FhirPath.Sprache;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.OperationOutcome;

namespace Firely.Validation.Compilation
{
    internal static class SchemaConverterExtensions
    {
        public static ElementSchema Convert(this ElementDefinition def)
        {
            var elements = new List<IAssertion>()
                .maybeAdd(def, buildMaxLength)
                .MaybeAdd(BuildFixed(def))
                .MaybeAdd(BuildPattern(def))
                .MaybeAdd(BuildBinding(def))
                .MaybeAdd(buildMinValue(def))
                .MaybeAdd(buildMaxValue(def))
                .MaybeAdd(buildFp(def))
                .MaybeAdd(buildCardinality(def))
                .MaybeAdd(buildElementRegEx(def))
                .MaybeAdd(buildTypeRefRegEx(def))
                .MaybeAdd(BuildTypeRefValidation(def))
               ;

            return new ElementSchema(id: new Uri("#" + def.ElementId ?? def.Path, UriKind.Relative), elements);
        }

        public static IAssertion? BuildBinding(ElementDefinition def)
            => def.Binding is not null ? new BindingAssertion(def.Binding.ValueSet, convertStrength(def.Binding.Strength), true, def.Binding.Description) : null;

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
                list.MaybeAdd(buildRegex(type));
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

        public static IAssertion? BuildFixed(ElementDefinition def) =>
            def.Fixed != null ? new Fixed(def.Fixed.ToTypedElement()) : null;

        public static IAssertion? BuildPattern(ElementDefinition def) =>
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

            var id = $"#{def.ElementId ?? def.Path}#constraints";
            return list.Any() ? new ElementSchema(id: new Uri(id, UriKind.Relative), list) : null;

            static IssueSeverity? convertConstraintSeverity(ElementDefinition.ConstraintSeverity? constraintSeverity) => constraintSeverity switch
            {
                ElementDefinition.ConstraintSeverity.Error => IssueSeverity.Error,
                ElementDefinition.ConstraintSeverity.Warning => IssueSeverity.Warning,
                _ => default,
            };
        }

        private static IAssertion? buildCardinality(ElementDefinition def) =>
            def.Min is null && (def.Max is null || def.Max == "*") ?
                    null :
                    CardinalityAssertion.FromMinMax(def.Min, def.Max, def.Path);

        private static IAssertion? buildRegex(IExtendable elementDef)
        {
            var pattern = elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/regex");
            return pattern != null ? new RegExAssertion(pattern) : null;
        }

        public static IAssertion? BuildTypeRefValidation(this ElementDefinition def) =>
            TypeReferenceConverter.ConvertTypeReferences(def.Type);

        public static IAssertion GroupAll(this IEnumerable<IAssertion> assertions, IAssertion? emptyAssertion = null)
        {
            // No use having a SUCCESS in an all, so we can optimize.
            var optimizedList = assertions.Where(a => a != ResultAssertion.SUCCESS).ToList();

            return optimizedList switch
            {
                { Count: 0 } => emptyAssertion ?? ResultAssertion.SUCCESS,
                { Count: 1 } list => list.Single(),
                var list => new AllAssertion(list)
            };
        }

        public static IAssertion GroupAny(this IEnumerable<IAssertion> assertions, IAssertion? emptyAssertion = null)
        {
            var listOfAssertions = assertions.ToList();

            // If any of the list is a success, we can just return a success
            if (listOfAssertions.Any(a => a == ResultAssertion.SUCCESS)) return ResultAssertion.SUCCESS;

            return assertions.ToList() switch
            {
                { Count: 0 } => emptyAssertion ?? ResultAssertion.SUCCESS,
                { Count: 1 } list => list.Single(),
                var list => new AnyAssertion(list)
            };
        }


        public static List<IAssertion> MaybeAdd(this List<IAssertion> assertions, IAssertion? element)
        {
            if (element is not null)
                assertions.Add(element);

            return assertions;
        }

        private static List<IAssertion> maybeAdd(this List<IAssertion> assertions, ElementDefinition def, Func<ElementDefinition, IAssertion?> builder)
        {
            // TODO: handle "compile" exceptions
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
            return assertions.MaybeAdd(element!);
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
