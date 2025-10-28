using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// An assertion which validates the FhirPath rules of the type only.
/// </summary>
/// <remarks>
/// This is a temporary hack for the issue where snapshot generator won't copy the invariants from base when pulling all children into the ElementDefinitionNavigator.
///
/// Extra details in this issue https://github.com/FirelyTeam/firely-validator-api/issues/491#issuecomment-2897145768
/// Remove once https://github.com/FirelyTeam/firely-net-sdk/issues/3156 is solved
/// </remarks>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class BaseTypeInvariantConstraintsValidator : IValidatable
{
    /// <summary>
    /// Validate input against the expected context and invariants.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="vc"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public ResultReport Validate(PocoNode input, ValidationSettings vc, ValidationState state)
    {
        var typeProfile = vc.TypeNameMapper.MapTypeName(input.Poco.TypeName);
        return FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, state, typeProfile, input.GetLocation()) switch
        {
            (var schema, null, _) => ResultReport.Combine(schema!.Members.Where(vc.Filter).OfType<FhirPathValidator>().Select(x => x.ValidateOne(input, vc, state)).ToList()),
            (_, var error, _) => error
        };
    }

    /// <summary>
    /// Converts this instance to a JSON token representing the base type invariant constraints.
    /// </summary>
    /// <return>A JToken representing the JSON structure for base type invariants.</return>
    public JToken ToJson() => new JProperty("baseTypeInvariants", new JObject());
}