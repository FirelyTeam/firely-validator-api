using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Shared.Correctors
{
    internal class CorrectorFactory
    {
        private static readonly Lazy<Corrector> RESOURCE_CORRECTOR = new(() => new ResourceCorrector());

        private static readonly Dictionary<string, Lazy<Corrector>> TYPE_CORRECTORS = new()
        {
            { "string", new Lazy<Corrector>(() => new StringCorrector()) },
            { "markdown", new Lazy<Corrector>(() => new MarkdownCorrector()) },
            { "Bundle", new Lazy<Corrector>(() => new BundleCorrector()) },
#if R4_AND_LATER
            { "CareTeam", new Lazy<Corrector>(() => new CareTeamCorrector()) },
            { "ElementDefinition", new Lazy<Corrector>(() => new ElementDefinitionCorrector()) },
            { "ImagingSelection", new Lazy<Corrector>(() => new ImagingSelectionCorrector()) },
            { "OperationDefinition", new Lazy<Corrector>(() => new OperationDefinitionCorrector()) },
#endif
            { "Observation", new Lazy<Corrector>(() => new ObservationCorrector()) },
            { "Reference", new Lazy<Corrector>(() => new ReferenceCorrector()) },
#if R4_AND_LATER
            { "StructureDefinition", new Lazy<Corrector>(() => new StructureDefinitionCorrector()) },
            { "Questionnaire", new Lazy<Corrector>(() => new QuestionnaireCorrector()) },
#endif
        };

        public static Corrector? Get(StructureDefinition.StructureDefinitionKind? kind)
        {
            return kind == StructureDefinition.StructureDefinitionKind.Resource 
                ? RESOURCE_CORRECTOR.Value 
                : null;
        }

        public static Corrector? Get(string? type)
        {
            return type != null && TYPE_CORRECTORS.TryGetValue(type, out var typeCorrector)
                ? typeCorrector.Value
                : null;
        }
    }
}
