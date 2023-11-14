/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Base class for simple validators that have only a single property to configure.
    /// </summary>
    public abstract class BasicValidator : IValidatable
    {
        /// <inheritdoc />
        public virtual JToken ToJson() => new JProperty(Key, Value);

        /// <inheritdoc />
        public abstract ResultReport Validate(IScopedNode input, ValidationContext vc, ValidationState state);

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
