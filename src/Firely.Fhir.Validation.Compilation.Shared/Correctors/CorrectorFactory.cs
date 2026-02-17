using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation.Shared.Correctors
{
    internal class CorrectorFactory
    {
        private static readonly Dictionary<StructureDefinition.StructureDefinitionKind, Lazy<Corrector>> KIND_CORRECTORS = new()
        {
            { StructureDefinition.StructureDefinitionKind.Resource, new Lazy<Corrector>(() => new ResourceCorrector()) }
        };

        private static readonly Dictionary<string, Lazy<Corrector>> TYPE_CORRECTORS = new()
        {
            { "string", new Lazy<Corrector>(() => new StringCorrector()) },
            { "markdown", new Lazy<Corrector>(() => new MarkdownCorrector()) },
            { "Bundle", new Lazy<Corrector>(() => new BundleCorrector()) },
            { "CareTeam", new Lazy<Corrector>(() => new CareTeamCorrector()) },
            { "ElementDefinition", new Lazy<Corrector>(() => new ElementDefinitionCorrector()) },
#if R4_AND_LATER
            { "ImagingSelection", new Lazy<Corrector>(() => new ImagingSelectionCorrector()) },
            { "OperationDefinition", new Lazy<Corrector>(() => new OperationDefinitionCorrector()) },
#endif
            { "Observation", new Lazy<Corrector>(() => new ObservationCorrector()) },
            { "Reference", new Lazy<Corrector>(() => new ReferenceCorrector()) },
            { "StructureDefinition", new Lazy<Corrector>(() => new StructureDefinitionCorrector()) },
            { "Questionnaire", new Lazy<Corrector>(() => new QuestionnaireCorrector()) },
        };

        public static Corrector? Get(StructureDefinition.StructureDefinitionKind? kind)
        {
            return kind.HasValue && KIND_CORRECTORS.TryGetValue(kind.Value, out var kindCorrector)
                ? kindCorrector.Value
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
