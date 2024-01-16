/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Base class for simple validators that have only a single property to configure.
    /// </summary>
    internal abstract class BasicValidator : IValidatable
    {
        /// <inheritdoc />
        public virtual JToken ToJson() => new JProperty(Key, Value);

        /// <inheritdoc />
        public abstract ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state);

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
