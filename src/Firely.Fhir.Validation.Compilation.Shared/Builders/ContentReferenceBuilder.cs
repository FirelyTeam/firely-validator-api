using Hl7.Fhir.Specification.Navigation;
using System.Collections.Generic;

namespace Firely.Fhir.Validation.Compilation
{
    /// <summary>
    ///
    /// </summary>
    /// <remarks>
    /// If this element has child constraints, then we don't need to
    /// add a reference to the unconstrained base definition of the element,
    /// since the snapshot generated will have added all constraints from
    /// the base definition to this element...unless the typeref adds
    /// additional details like profiles or targetProfiles on top of the basic
    /// type.
    /// </remarks>
    internal class ContentReferenceBuilder : ICompilerExtension
    {
        public IEnumerable<IAssertion> Build(ElementDefinitionNavigator nav, ElementConversionMode? conversionMode = ElementConversionMode.Full)
        {
            var def = nav.Current;

            if (nav.HasChildren || def.ContentReference is null) yield break;

            // The content reference looks like http://hl7.org/fhir/StructureDefinition/Patient#Patient.x.y),
            // OR just #Patient.x.y
            if (def.ContentReference.StartsWith("#"))
            {
                var datatype = def.ContentReference[(def.ContentReference.IndexOf("#") + 1)..].Split('.')[0];
                yield return new SchemaReferenceValidator(Canonical.ForCoreType(datatype).Uri + def.ContentReference);
            }
            else
                yield return new SchemaReferenceValidator(def.ContentReference);

        }
    }
}
