using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// An assertion which validates the FhirPath rules of the type only.
/// </summary>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
internal class BaseTypeInvariantConstraintsValidator : IValidatable
{
    /// <summary>
    /// Validate input against the expected context and invariants.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="vc"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
    {
        if(input.InstanceType is null)
            throw new ArgumentException($"Cannot validate the resource because {nameof(IScopedNode)} does not have an instance type.");
        
        return FhirSchemaGroupAnalyzer.FetchSchema(vc.ElementSchemaResolver, state, vc.TypeNameMapper.MapTypeName(input.InstanceType)) switch
        {
            (var schema, null, _) => ResultReport.Combine(schema!.Members.OfType<FhirPathValidator>().Select(x => x.ValidateOne(input, vc, state)).ToList()),
            (_, var error, _) => error
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public JToken ToJson() => new JProperty("baseTypeInvariants", new JObject());
}