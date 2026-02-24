using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System.Linq;
using System.Text.RegularExpressions;

namespace Firely.Fhir.Validation.Compilation;

internal partial class ResourceCorrector : Corrector
{
    private const string FHIR_TYPE_EXTENSION = "http://hl7.org/fhir/StructureDefinition/structuredefinition-fhir-type";
    private const string ID_REGEX_PATTERN = @"^[a-zA-Z]+\.id$";

    // Optimize id regex matching
#if NET8_0_OR_GREATER
    [GeneratedRegex(ID_REGEX_PATTERN)]
    private static partial Regex idRegEx();
#else
    private static Regex? _idRegex;
    private static Regex idRegEx() => _idRegex ??= new Regex(ID_REGEX_PATTERN, RegexOptions.Compiled);
#endif

    public override void Correct(FhirRelease? fhirRelease, StructureDefinition sd)
    {
        correctIdElement(sd.Differential);
        correctIdElement(sd.Snapshot);
    }

    private static void correctIdElement(IElementList? elements)
    {
        if (elements is null) 
            return;

        // Take 2 to make sure we do not iterate over all matching elements since we are only checking on count not equaling 1!
        var idElements = elements.Element.Where(e => idRegEx().IsMatch(e.Path!)).Take(2);

        if (idElements.Count() != 1 || idElements.First().Type is not { Count: 1 } singleTypeRef || singleTypeRef[0].Code == "id")
            return;

        singleTypeRef[0].Code = "id";

        // Update fhir type extension if it is present
        var fhirTypeExtensions = singleTypeRef[0].Extension.Where(e => e.Url == FHIR_TYPE_EXTENSION);

        foreach (var extension in fhirTypeExtensions)
        {
            if (extension.Value is IValue<string> { Value: "string" } stringValue)
                stringValue.Value = "id";
        }
    }
}