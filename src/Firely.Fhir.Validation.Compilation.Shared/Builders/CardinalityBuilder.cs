using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    internal class CardinalityBuilder : ICompilerExtension
    {
        private static readonly string EXTENSION_TYPE_NAME = ModelInspector.Base.GetFhirTypeNameForType(typeof(Extension))!;

        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            // This constraint is part of an element (whether referring to a backbone type or not),
            // so this should not be part of the type generated for a backbone (see eld-5).
            // Note: the snapgen will ensure this constraint is copied over from the referred
            // element to the referring element (= which has a contentReference).
            if (conversionMode == ElementConversionMode.BackboneType) yield break;

            var def = nav.Current;

            // Avoid generating cardinality checks on the root of resources and datatypes,
            // except for Extensions
            if (!def.Path.Contains('.') && def.Path != EXTENSION_TYPE_NAME) yield break;

            if (def.Min is not null || def.Max == "*")
                yield return CardinalityValidator.FromMinMax(def.Min, def.Max);
        }

    }
}
