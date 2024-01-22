/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Base class for simple validators that have only a single property to configure.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.Experimental(diagnosticId: "ExperimentalApi")]
#else
    [System.Obsolete("This function is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.")]
#endif
    public abstract class BasicValidator : IValidatable
    {
        /// <inheritdoc />
        public virtual JToken ToJson() => new JProperty(Key, Value);

        /// <inheritdoc />
        ResultReport IValidatable.Validate(IScopedNode input, ValidationSettings vc, ValidationState state) =>
            BasicValidate(input, vc, state);

        internal abstract ResultReport BasicValidate(IScopedNode input, ValidationSettings vc, ValidationState state);


        /// <summary>
        /// The name of the property used in the json serialization for this validator."
        /// </summary>
        protected abstract string Key { get; }

        /// <summary>
        /// The value of the property used in the json serialization for this validator."
        /// </summary>
        protected abstract object Value { get; }
    }
}
