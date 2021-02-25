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


        // Turns a TypeRef element into a set of assertions according to this general plan:
        /*
         * Identifier:[][]
         * HumanName:[HumanNameDE,HumanNameBE]:[]
         * Reference:[WithReqDefinition,WithIdentifier]:[Practitioner,OrganizationBE]
         * 
         * Any
         * {
         *     switch
         *     [TypeLabel Identifier] {
         *          ref: "http://hl7.org/SD/Identifier"
         *     },
         *     [TypeLabel HumanName]
         *     {
         *          Any { ref: "HumanNameDE", ref: "HumanNameBE" }
         *     },
         *     [TypeLabel Reference]
         *     {
         *          Any { ref: "WithReqDefinition", ref: "WithIdentifier" }
         *          Any 
         *          { 
         *              validate: resolve-from("reference") against [http://hl7.org/fhir/SD/Practitioner],
         *              validate: resolve-from("reference") against [http://example.org/OrganizationBE] 
         *          } 
         *     }
         * }
         */
        public static IAssertion? BuildTypeRefValidation(this ElementDefinition def)
        {
            var builder = new TypeCaseBuilder();

            // Note, in R3, this can be empty for system primitives (so the .value element of datatypes)
            if (def.Type.Any(t => string.IsNullOrEmpty(t.Code)))
                throw new IncorrectElementDefinitionException($"Encountered a typeref without a code at {def.Path}");

            // In R4, all Codes must be unique (in R3, this was seen as an OR)
            if (def.Type.Select(t => t.Code).Distinct().Count() != def.Type.Count)
                throw new IncorrectElementDefinitionException($"Encountered an element with typerefs with non-unique codes at {def.Path}");

            var typeRefs = from tr in def.Type
                           let profiles = tr.GetDeclaredProfiles()
                           select (code: tr.Code, profiles, tr.Aggregation.Where(a => a is not null).Select(a => convertAggregationMode(a!.Value)));

            //Distinguish between:
            // * elem with a reference to a system type - end of validation - do nothing  => must be done in SchemaRef assertion
            // * elem with a single TypeRef - does not need any slicing
            // * genuine choice elements (suffix [x]) - needs to be sliced on FhirTypeLabel 
            // * elem with multiple TypeRefs - without explicit suffix [x], this is a slice 
            // without discriminator

            // Determine whether we need a type slice.  This is *not* just needed when this is a choice element (suffix [x]), but
            // really for any "polmorph" element, which includes elements of type (Domain)Resource. Checking whether there is
            // more than one Code in the typeref is also not enough, since it is nice to verify the type used in a choice element,
            // even if it is constrained down to a single choice. So: we want slicing when an element is a choice element AND
            // when there is more than one distinct Code in the list of typerefs. 
            var needsTypeSlicing = isChoice(def) || def.Type.Select(tr => tr.Code).Count() > 1;

            if (needsTypeSlicing)
            {
                var typeCases = typeRefs
                    .GroupBy(tr => tr.code)
                    .Select(tc => (code: tc.Key, profiles: tc.SelectMany(dp => dp.profiles)));

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
            static bool isChoice(ElementDefinition d) => d.Base?.Path?.EndsWith("[x]") == true ||
                                d.Path.EndsWith("[x]");
        }

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
