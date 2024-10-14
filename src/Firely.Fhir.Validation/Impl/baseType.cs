using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation;

/// <summary>
/// Not an actual assertion. Contains the type code for the element when no type reference was created.
/// </summary>
[DataContract]
[EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
[System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
public class BaseType : IAssertion
{
    /// <summary>
    /// Create a new baseType.
    /// </summary>
    /// <param name="baseRef"></param>
    public BaseType(string baseRef)
    {
        this.Type = baseRef;
    }
    
    /// <summary>
    /// The type code for the element when no type reference was created.
    /// </summary>
    public string Type { get; }
    
    /// <inheritdoc />
    public JToken ToJson() => new JProperty("baseRef", Type);
}