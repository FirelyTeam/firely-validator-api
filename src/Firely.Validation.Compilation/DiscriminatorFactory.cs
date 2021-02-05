using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;
using System;
using System.Linq;

namespace Firely.Validation.Compilation
{
    public class DiscriminatorFactory
    {
        public static IAssertion Build(ElementDefinitionNavigator root, ElementDefinition.DiscriminatorComponent discriminator,
            IAsyncResourceResolver? resolver)
        {
            if (discriminator?.Type == null) throw new ArgumentNullException(nameof(discriminator), "Encountered a discriminator component without a discriminator type.");
            if (resolver == null) throw Error.ArgumentNull(nameof(resolver));

            var condition = walkToCondition(root, discriminator.Path, resolver);
            var location = root.Current.Path;

            var discrimatorAssertion = discriminator.Type.Value switch
            {
                ElementDefinition.DiscriminatorType.Value => buildCombinedDiscriminator("value", condition.Current),
                ElementDefinition.DiscriminatorType.Pattern => buildCombinedDiscriminator("pattern", condition.Current),
                ElementDefinition.DiscriminatorType.Type => buildTypeDiscriminator(condition.Current),
                ElementDefinition.DiscriminatorType.Profile => buildProfileDiscriminator(condition.Current),
                ElementDefinition.DiscriminatorType.Exists => buildExistsDiscriminator(condition.Current),
                _ => throw Error.NotImplemented($"Found a slice discriminator of type '{discriminator.Type.Value.GetLiteral()}' at '{location}' which is not yet supported by this validator."),
            };

            return new PathSelectorAssertion(discriminator.Path, discrimatorAssertion);
        }

        private static IAssertion buildExistsDiscriminator(ElementDefinition current)
        {
            return ResultAssertion.SUCCESS;
        }

        private static IAssertion buildCombinedDiscriminator(string name, ElementDefinition spec)
            => spec.Fixed == null && spec.Binding == null && spec.Pattern == null
                ? throw new IncorrectElementDefinitionException($"The {name} discriminator should have a 'fixed[x]', 'pattern[x]' or binding element set on '{spec.ElementId}'.")
                : spec.ValueSlicingConditions();

        private static IAssertion buildTypeDiscriminator(ElementDefinition spec)
        {
            var codes = spec.Type.Select(tr => tr.Code).ToArray();

            return codes.Any()
                ? (IAssertion)new AnyAssertion(codes.Select(c => new FhirTypeLabel(c)))
                : throw new IncorrectElementDefinitionException($"A type discriminator should have at least one 'type' element with a code set on '{spec.ElementId}'.");
        }

        private static IAssertion buildProfileDiscriminator(ElementDefinition spec)
        {
            throw new NotImplementedException();
        }

        private static ElementDefinitionNavigator walkToCondition(ElementDefinitionNavigator root, string discriminator, IAsyncResourceResolver resolver)
        {
            var walker = new StructureDefinitionWalker(root, resolver);
            var conditions = walker.Walk(discriminator);

            if (!conditions.Any())
                throw new IncorrectElementDefinitionException("$The discriminator path '{discriminator}' at { root.CanonicalPath() } leads to no ElementDefinitions, which is not allowed.");

            // Well, we could check whether the conditions are Equal, since that's what really matters - they should not differ.
            return conditions.Count > 1
                ? throw new IncorrectElementDefinitionException($"The discriminator path '{discriminator}' at {root.CanonicalPath()} leads to multiple ElementDefinitions, which is not allowed.")
                : conditions.Single().Current;
        }

    }
}
