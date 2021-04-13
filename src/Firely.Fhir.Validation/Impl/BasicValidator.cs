/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    public abstract class BasicValidator : IValidatable
    {
        public virtual JToken ToJson() => new JProperty(Key, Value);

        public abstract Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state);

        public abstract string Key { get; }
        public abstract object Value { get; }
    }
}
