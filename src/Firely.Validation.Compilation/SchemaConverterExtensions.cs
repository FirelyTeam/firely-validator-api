﻿/* 
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
        public static IElementSchema Convert(this ElementDefinition def, ISchemaResolver resolver, IElementDefinitionAssertionFactory assertionFactory)
        {

            var elements = new List<IAssertion>()
                .MaybeAdd(def, assertionFactory, buildMaxLength)
                .MaybeAdd(buildFixed(def, assertionFactory))
                .MaybeAdd(buildPattern(def, assertionFactory))
                .MaybeAdd(buildBinding(def, assertionFactory))
                .MaybeAdd(buildMinValue(def, assertionFactory))
                .MaybeAdd(buildMaxValue(def, assertionFactory))
                .MaybeAdd(buildFp(def, assertionFactory))
                .MaybeAdd(buildCardinality(def, assertionFactory))
                .MaybeAdd(buildElementRegEx(def, assertionFactory))
                .MaybeAdd(buildTypeRefRegEx(def, assertionFactory))
                .MaybeAdd(BuildTypeRefValidation(def, resolver, assertionFactory))
               ;

            return assertionFactory.CreateElementSchemaAssertion(id: new Uri("#" + def.Path, UriKind.Relative), elements);
        }

        public static IAssertion ValueSlicingConditions(this ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory)
        {
            var elements = new List<IAssertion>()
                   .MaybeAdd(buildFixed(def, assertionFactory))
                   .MaybeAdd(buildPattern(def, assertionFactory))
                   .MaybeAdd(buildBinding(def, assertionFactory));

            return new AllAssertion(elements);
        }

        private static IAssertion? buildBinding(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory)
            => def.Binding is not null ? assertionFactory.CreateBindingAssertion(def.Binding.ValueSet, convertStrength(def.Binding.Strength), false, def.Binding.Description) : null;

        private static BindingAssertion.BindingStrength? convertStrength(BindingStrength? strength) => strength switch
        {
            BindingStrength.Required => BindingAssertion.BindingStrength.Required,
            BindingStrength.Extensible => BindingAssertion.BindingStrength.Extensible,
            BindingStrength.Preferred => BindingAssertion.BindingStrength.Preferred,
            BindingStrength.Example => BindingAssertion.BindingStrength.Example,
            _ => default,
        };

        private static IAssertion? buildTypeRefRegEx(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory)
        {
            var list = new List<IAssertion>();

            foreach (var type in def.Type)
            {
                list.MaybeAdd(buildRegex(type, assertionFactory));
            }
            return list.Count > 0 ? assertionFactory.CreateElementSchemaAssertion(id: new Uri("#" + def.Path, UriKind.Relative), list) : null;
        }

        private static IAssertion? buildElementRegEx(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
            buildRegex(def, assertionFactory);

        private static IAssertion? buildMinValue(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
            def.MinValue != null ? assertionFactory.CreateMinMaxValueAssertion(def.MinValue.ToTypedElement(), MinMax.MinValue) : null;

        private static IAssertion? buildMaxValue(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
            def.MaxValue != null ? assertionFactory.CreateMinMaxValueAssertion(def.MaxValue.ToTypedElement(), MinMax.MaxValue) : null;

        private static IAssertion? buildFixed(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
            def.Fixed != null ? assertionFactory.CreateFixedValueAssertion(def.Fixed.ToTypedElement()) : null;

        private static IAssertion? buildPattern(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
           def.Pattern != null ? assertionFactory.CreatePatternAssertion(def.Pattern.ToTypedElement()) : null;

        private static IAssertion? buildMaxLength(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
            def.MaxLength.HasValue ? assertionFactory.CreateMaxLengthAssertion(def.MaxLength.Value) : null;

        private static IAssertion? buildFp(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory)
        {
            var list = new List<IAssertion>();
            foreach (var constraint in def.Constraint)
            {
                var bestPractice = constraint.GetBoolExtension("http://hl7.org/fhir/StructureDefinition/elementdefinition-bestpractice") ?? false;
                list.Add(assertionFactory.CreateFhirPathAssertion(constraint.Key, constraint.Expression, constraint.Human, convertConstraintSeverity(constraint.Severity), bestPractice));
            }

            return list.Any() ? assertionFactory.CreateElementSchemaAssertion(id: new Uri("#constraints", UriKind.Relative), list) : null;

            static IssueSeverity? convertConstraintSeverity(ElementDefinition.ConstraintSeverity? constraintSeverity) => constraintSeverity switch
            {
                ElementDefinition.ConstraintSeverity.Error => IssueSeverity.Error,
                ElementDefinition.ConstraintSeverity.Warning => IssueSeverity.Warning,
                _ => default,
            };
        }

        private static IAssertion? buildCardinality(ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory) =>
            def.Min != null || def.Max != null ? assertionFactory.CreateCardinalityAssertion(def.Min, def.Max, def.Path) : null;

        private static IAssertion? buildRegex(IExtendable elementDef, IElementDefinitionAssertionFactory assertionFactory)
        {
            var pattern = elementDef?.GetStringExtension("http://hl7.org/fhir/StructureDefinition/regex");
            return pattern != null ? assertionFactory.CreateRegexAssertion(pattern) : null;
        }

        // TODO this should be somewhere else
        private static AggregationMode? convertAggregationMode(ElementDefinition.AggregationMode? aggregationMode)
            => aggregationMode switch
            {
                ElementDefinition.AggregationMode.Bundled => AggregationMode.Bundled,
                ElementDefinition.AggregationMode.Contained => AggregationMode.Contained,
                ElementDefinition.AggregationMode.Referenced => AggregationMode.Referenced,
                _ => (AggregationMode?)null
            };

        public static IAssertion? BuildTypeRefValidation(this ElementDefinition def, ISchemaResolver resolver, IElementDefinitionAssertionFactory assertionFactory)
        {
            var builder = new TypeCaseBuilder(resolver, assertionFactory);

            var typeRefs = from tr in def.Type
                           let profile = tr.GetDeclaredProfiles()
                           where profile != null
                           select (code: tr.Code, profile, tr.Aggregation.Select(a => convertAggregationMode(a)));

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
                    result += new AnyAssertion(profile.Select(p => builder.BuildProfileRef(code, p, aggregations)));
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

        private static List<IAssertion> MaybeAdd(this List<IAssertion> assertions, ElementDefinition def, IElementDefinitionAssertionFactory assertionFactory, Func<ElementDefinition, IElementDefinitionAssertionFactory, IAssertion?> builder)
        {
            // TODOL handle "compile" exceptions
            IAssertion? element = null;
            try
            {
                element = builder(def, assertionFactory);
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
