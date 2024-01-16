/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation.Compilation
{
    internal class DiscriminatorFactory
    {

        /// <summary>
        /// Given the root of the set of constraints for a slice and the discriminator, build an assertion that would return
        /// success when run against an instance that would match that slice. This assertion can be used as the condition
        /// in the <see cref="SliceValidator"/> to group instances to validate against the assertions for that slice.
        /// </summary>
        public static IAssertion? Build(ElementDefinitionNavigator root, ElementDefinition.DiscriminatorComponent discriminator,
        IAsyncResourceResolver? resolver)
        {
            if (discriminator?.Type == null) throw new ArgumentNullException(nameof(discriminator), "Encountered a discriminator component without a discriminator type.");
            if (resolver == null) throw Error.ArgumentNull(nameof(resolver));

            var condition = walkToCondition(root, discriminator.Path, resolver);
            if (condition is null) return null;

            var location = root.Current.Path;

            var discrimatorAssertion = discriminator.Type.Value switch
            {
                ElementDefinition.DiscriminatorType.Value => buildCombinedDiscriminator("value", condition),
                ElementDefinition.DiscriminatorType.Pattern => buildCombinedDiscriminator("pattern", condition),
                ElementDefinition.DiscriminatorType.Type => buildTypeDiscriminator(condition, discriminator.Path),
                ElementDefinition.DiscriminatorType.Profile => buildProfileDiscriminator(condition, discriminator.Path),
                ElementDefinition.DiscriminatorType.Exists => buildExistsDiscriminator(condition.Current),
                _ => throw Error.NotImplemented($"Found a slice discriminator of type '{discriminator.Type.Value.GetLiteral()}' at '{location}' which is not yet supported by this validator."),
            };

            // If the discriminator is always true, don't even go out to get the discriminated value
            return discrimatorAssertion.IsAlways(ValidationResult.Success)
                ? ResultAssertion.SUCCESS
                : new PathSelectorValidator(discriminator.Path, discrimatorAssertion);
        }

        private static IAssertion buildExistsDiscriminator(ElementDefinition spec)
        {
            // For an exists discriminator, whether an instance matches the min/max constraint is what determines the 
            // case to be used. Normally, ElementDefinition.Min will be "1" for the "exists" case, and ElementDefinition.Max
            // will be "0" for the non-exists case. I've taken it a bit more generally, re-using the CardinalityAssertion,
            // so you could even use Min = 2... or a specific max.  The spec is very unclear about this, I've filed an
            // issue about it (https://jira.hl7.org/browse/FHIR-31603)
            return CardinalityValidator.FromMinMax(spec.Min, spec.Max);
        }

        private static IAssertion buildCombinedDiscriminator(string name, ElementDefinitionNavigator nav)
        {
            var spec = nav.Current;
            return spec.Fixed == null && spec.Binding == null && spec.Pattern == null
                ? throw new IncorrectElementDefinitionException($"The {name} discriminator should have a 'fixed[x]', 'pattern[x]' or binding element set on '{spec.ElementId}'.")
                : buildValueSlicingConditions(nav);

            // Based on the changes proposed in https://jira.hl7.org/browse/FHIR-25206,
            // we have now implemented this discriminator as using *any* combination of fixed/pattern/binding.
            // This is incompatible with R3/R4, however, where we might need to compile this into either
            // fixed (having EITHER fixed or binding) OR pattern (with pattern OR binding??), as was the old behaviour.
            // Since the description of the old behaviour (http://hl7.org/fhir/profiling.html#discriminator) is unclear,
            // and this was ambiguously implemented across validators, we might as well decide to handle this the "R5-way",
            // also in the older versions.
            static IAssertion buildValueSlicingConditions(ElementDefinitionNavigator nav)
            {
                var elements = new List<IAssertion>()
                    .MaybeAddMany(new FixedBuilder().Build(nav))
                    .MaybeAddMany(new PatternBuilder().Build(nav))
                    .MaybeAddMany(new BindingBuilder().Build(nav));

                return elements.GroupAll();
            }
        }

        private static IAssertion buildTypeDiscriminator(ElementDefinitionNavigator nav, string discriminator)
        {
            var spec = nav.Current;

            if (spec.IsRootElement())
            {
                // Firsts case: we are at the root of a StructureDefinition, most commonly because
                // the discriminator path ended in a resolve(). We need to find the canonical url
                // for the type of the thing we've landed on, since we're at the root of the profile we actually
                // want to validate against.
                var profile = nav.StructureDefinition?.Type ??
                    throw new InvalidOperationException($"Cannot determine the type of the element at '{nav.CanonicalPath()}' - parent StructureDefinition was not set on navigator.");

                return new FhirTypeLabelValidator(profile);
            }
            else
            {
                // Second case: we are inside a structure definition (not on the root), in this case
                // the current element can only be checked by the types in the Code on the <type> element.
                // Note that the element pointed to by the discriminator should have constrained the types
                // to a single (unique) type for this to work.
                var distinctCodes = spec.Type.Select(tr => tr.Code).Distinct().ToArray();
                return distinctCodes.Length == 1
                    ? new FhirTypeLabelValidator(distinctCodes[0])
                    : throw new IncorrectElementDefinitionException($"The type discriminator '{discriminator}' should navigate to an ElementDefinition with exactly one 'type' element at '{nav.CanonicalPath()}'.");
            }
        }

        private static IAssertion buildProfileDiscriminator(ElementDefinitionNavigator nav, string discriminator)
        {
            var spec = nav.Current;

            if (spec.IsRootElement())
            {
                // Firsts case: we are at the root of a StructureDefinition, most commonly because
                // the discriminator path ended in a resolve(). We need to find the canonical url
                // of the thing we've landed on, since we're at the root of the profile we actually
                // want to validate against.
                var profile = nav.StructureDefinition?.Url ??
                    throw new InvalidOperationException($"Cannot determine the canonical url for the profile at '{nav.CanonicalPath()}' - parent StructureDefinition was not set on navigator.");

                return new SchemaReferenceValidator(profile);
            }
            else
            {
                // Second case: we are inside a structure definition (not on the root), in this case
                // the current element can only be profiled by the <profile> tag(s) on the <type> element.
                // Note that the element pointed to by the discriminator should have constrained the types
                // to a single (unique) type, but we will allow multiple <profile>s.
                if (spec.Type.Select(tr => tr.Code).Distinct().Count() != 1)   // STU3, in R4 codes are always unique
                    throw new IncorrectElementDefinitionException($"The profile discriminator '{discriminator}' should navigate to an ElementDefinition with exactly one 'type' element at '{nav.CanonicalPath()}'.");
#if STU3
                var profiles = spec.Type.Where(t => t.Profile is not null).Select(tr => tr.Profile).Distinct();
#else
                var profiles = spec.Type.Where(t => t.Profile.Any()).SelectMany(tr => tr.Profile).Distinct();
#endif
                return profiles.Select(p => new SchemaReferenceValidator(p)).GroupAny();
            }
        }

        private static ElementDefinitionNavigator? walkToCondition(ElementDefinitionNavigator root, string discriminator, IAsyncResourceResolver resolver)
        {
            var walker = new StructureDefinitionWalker(root, resolver);
            var conditions = walker.Walk(discriminator);

            if (!conditions.Any())
                return null;

            // Well, we could check whether the conditions are Equal, since that's what really matters - they should not differ.
            return conditions.Count > 1
                ? throw new IncorrectElementDefinitionException($"The discriminator path '{discriminator}' at {root.CanonicalPath()} leads to multiple ElementDefinitions, which is not allowed.")
                : conditions.Single().Current;
        }

    }
}
