/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.FhirPath;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// Asserts another assertion on a subset of an instance given by a FhirPath expression. Used primarily for discriminating
    /// the cases of a <see cref="SliceValidator"/>.
    /// </summary>
    [DataContract]
    public class PathSelectorValidator : IValidatable
    {
#if MSGPACK_KEY
        [DataMember(Order = 0)]
        public string Path { get; private set; }

        [DataMember(Order = 1)]
        public IAssertion Other { get; private set; }
#else
        [DataMember]
        public string Path { get; private set; }

        [DataMember]
        public IAssertion Other { get; private set; }
#endif

        public PathSelectorValidator(string path, IAssertion other)
        {
            Path = path;
            Other = other;
        }

        public async Task<Assertions> Validate(ITypedElement input, ValidationContext vc, ValidationState state)
        {
            var selected = input.Select(Path).ToList();

            return selected switch
            {
                // 0, 1 or more results are ok for group validatables. Even an empty result is valid for, say, cardinality constraints.
                _ when Other is IGroupValidatable igv => await igv.Validate(selected, Path, vc, state).ConfigureAwait(false),

                // A non-group validatable cannot be used with 0 results.
                { Count: 0 } => new Assertions(
                    ResultAssertion.CreateFailure(
                        new TraceAssertion(input.Location, $"The FhirPath selector {Path} did not return any results."))),

                // 1 is ok for non group validatables
                { Count: 1 } => await Other.Validate(selected, selected.Single().Location, vc, state).ConfigureAwait(false),

                // Otherwise we have too many results for a non-group validatable.
                _ => new Assertions(
                    ResultAssertion.CreateFailure(
                        new TraceAssertion(input.Location, $"The FhirPath selector {Path} returned too many ({selected.Count}) results.")))
            };
        }

        public JToken ToJson()
        {
            var props = new JObject()
            {
                new JProperty("path", Path),
                new JProperty("assertion", new JObject(Other.ToJson()))

            };

            return new JProperty("pathSelector", props);
        }



    }
}
