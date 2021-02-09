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
using Hl7.FhirPath.Sprache;
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
                .MaybeAdd(def, buildMaxLength)
                .MaybeAdd(buildFixed(def))
                .MaybeAdd(buildPattern(def))
                .MaybeAdd(buildBinding(def))
                .MaybeAdd(buildMinValue(def))
                .MaybeAdd(buildMaxValue(def))
                .MaybeAdd(buildFp(def))
                .MaybeAdd(buildCardinality(def))
                .MaybeAdd(buildElementRegEx(def))
                .MaybeAdd(buildTypeRefRegEx(def))
                .MaybeAdd(BuildTypeRefValidation(def))
               ;

            return new ElementSchema(id: new Uri("#" + def.Path, UriKind.Relative), elements);
        }

        public static IAssertion ValueSlicingConditions(this ElementDefinition def)
        {
            var elements = new List<IAssertion>()
                   .MaybeAdd(buildFixed(def))
                   .MaybeAdd(buildPattern(def))
                   .MaybeAdd(buildBinding(def));

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

        private static IAssertion? buildTypeRefRegEx(ElementDefinition def)
        {
            var list = new List<IAssertion>();

            foreach (var type in def.Type)
            {
                list.MaybeAdd(buildRegex(type));
            }
            return list.Count > 0 ? new ElementSchema(id: new Uri("#" + def.Path, UriKind.Relative), list) : null;
        }

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
                list.Add(new FhirPathAssertion(constraint.Key, constraint.Expression, constraint.Human, convertConstraintSeverity(constraint.Severity), bestPractice));
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

        public static IAssertion? BuildTypeRefValidation(this ElementDefinition def)
        {
            var builder = new TypeCaseBuilder();

            var typeRefs = from tr in def.Type
                           let profile = tr.GetDeclaredProfiles()
                           where profile != null
                           select (code: tr.Code, profile, tr.Aggregation.Where(a => a is not null).Select(a => convertAggregationMode(a!.Value)));

            //Distinguish between:
            // * elem with a single TypeRef - does not need any slicing
            // * genuine choice elements (suffix [x]) - needs to be sliced on FhirTypeLabel 
            // * elem with multiple TypeRefs - without explicit suffix [x], this is a slice 
            // without discriminator

            //if (def.IsPrimitiveConstraint())
            // {
            //    return builder.BuildProfileRef("System.String", "http://hl7.org/fhir/StructureDefinition/System.String"); // TODO MV: this was profile and not profile.Single()
            //}

            /*
            var result = Assertions.Empty;
            foreach (var (code, profile) in typeRefs)
            {
                result += new AnyAssertion(profile.Select(p => builder.BuildProfileRef(code, p)));
            }
            return result.Count > 0 ? new AnyAssertion(result) : null;
            */

            /*
            if (def.Slicing != null)
            {
                return BuildSlicing(def);
            }
            */


            if (isChoice(def))
            {
                var typeCases = typeRefs
                    .GroupBy(tr => tr.code)
                    .Select(tc => (code: tc.Key, profiles: tc.SelectMany(dp => dp.profile)));

                return builder.BuildSliceAssertionForTypeCases(typeCases);
            }
            else
            {
                var result = Assertions.EMPTY;
                foreach (var (code, profile, aggregations) in typeRefs)
                {
                    result += new AnyAssertion(profile.Select(p => TypeCaseBuilder.BuildProfileRef(code, p, aggregations)));
                }
                return result.Count > 0 ? new AnyAssertion(result) : null;
            }
            /*else if (typeRefs.Count() == 1)
            {
                var (code, profile) = typeRefs.Single();
                var assertion = new FhirTypeLabel(code, "TODO");

                var profileAssertions = new AnyAssertion(profile.Select(p => builder.BuildProfileRef(code, p)));
                return new AllAssertion(assertion, profileAssertions);
            }
            else
                return new TraceText("TODO");*/
            //return builder.BuildSliceForProfiles(typeRefs.Select(tr => tr.profile));
            // return new AnyAssertion(typeRefs.SelectMany(t => t.profile.Select(p => builder.BuildProfileRef(t.code, p))));

            //if (typeRefs.Count() == 1)
            //    return builder.BuildProfileRef(typeRefs.Single().code, typeRefs.Single().profile.Single()); // TODO MV: this was profile and not profile.Single()


            /*
             * Identifier:[][]
             * HumanName:[HumanNameDE,HumanNameBE]:[]
             * Reference:[WithReqDefinition,WithIdentifier]:[Practitioner,OrganizationBE]
             * 
             * Any
             * {
             *     {
             *          InstanceType: "Identifier"
             *          ref: "http://hl7.org/SD/Identifier"
             *     }
             *     {
             *          InstanceType: "HumanName"
             *          Any { ref: "HumanNameDE", ref: "HumanNameBE" }
             *     },
             *     {
             *          InstanceType: "Reference"
             *           Any { ref: "WithReqDefinition", ref: "WithIdentifier" }
             *          Any { validate: [http://example4] [http://hl7.oerg/fhir/SD/Practitioner],
             *              validate: [http://example] [http://..../OrganizationBE] } 
             *     }
             * }
            /*
            if (isChoice(def))
            {
                var typeCases = typeRefs
                    .GroupBy(tr => tr.code)
                    .Select(tc => (code: tc.Key, profiles: tc.Select(dp => dp.profile)));

                return builder.BuildSliceAssertionForTypeCases(typeCases);
            }
            else if (typeRefs.Count() == 1)
                return builder.BuildProfileRef(typeRefs.Single().profile);
            else
                return builder.BuildSliceForProfiles(typeRefs.Select(tr => tr.profile));



            */
            //return null;
            bool isChoice(ElementDefinition d) => d.Base?.Path?.EndsWith("[x]") == true ||
                            d.Path.EndsWith("[x]");
        }

        private static List<IAssertion> MaybeAdd(this List<IAssertion> assertions, IAssertion? element)
        {
            if (element is not null)
                assertions.Add(element);

            return assertions;
        }

        private static List<IAssertion> MaybeAdd(this List<IAssertion> assertions, ElementDefinition def, Func<ElementDefinition, IAssertion?> builder)
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
