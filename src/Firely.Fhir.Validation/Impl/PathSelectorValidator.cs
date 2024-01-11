/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{

    /// <summary>
    /// Asserts another assertion on a subset of an instance given by a FhirPath expression. 
    /// Used internally only for discriminating the cases of a <see cref="SliceValidator"/>.
    /// </summary>
    [DataContract]
    internal class PathSelectorValidator : IValidatable
    {
        /// <summary>
        /// The FhirPath statement used to select a value to validate.
        /// </summary>
        [DataMember]
        public string Path { get; private set; }

        /// <summary>
        /// The assertion to run on the value produced by evaluating the <see cref="Path" />
        /// </summary>
        [DataMember]
        public IAssertion Other { get; private set; }

        /// <summary>
        /// Constructs a validator given the FhirPath and an assertion to run.
        /// </summary>
        public PathSelectorValidator(string path, IAssertion other)
        {
            Path = path;
            Other = other;
        }

        /// <inheritdoc/>
        /// <remarks>Note that this validator is only used internally to represent the checks for
        /// the path-based discriminated cases in a <see cref="SliceValidator" />, so this validator
        /// does not produce standard Issue-based errors.</remarks>
        public ResultReport Validate(IScopedNode input, ValidationSettings vc, ValidationState state)
        {
            initializeFhirPathCache(vc, state);

#pragma warning disable CS0618 // Type or member is obsolete
            var selected = state.Global.FPCompilerCache!.Select(input.AsTypedElement(), Path).ToList();
#pragma warning restore CS0618 // Type or member is obsolete

            if (selected.Any())
            {
                // Update the state with the location of the first selected element.
                // TODO: Actually the FhirPath Select statement should give us the location of the selected element.
                state = state.UpdateInstanceLocation(ip => ip.AddInternalReference(selected.First().Location));
            }

            var selectedScopedNodes = cast(selected);

            return selectedScopedNodes switch
            {
                // 0, 1 or more results are ok for group validatables. Even an empty result is valid for, say, cardinality constraints.
                _ when Other is IGroupValidatable igv => igv.Validate(selectedScopedNodes, vc, state),

                // A non-group validatable cannot be used with 0 results.
                { Count: 0 } => new ResultReport(ValidationResult.Failure,
                        new TraceAssertion(state.Location.InstanceLocation.ToString(), $"The FhirPath selector {Path} did not return any results.")),

                // 1 is ok for non group validatables
                { Count: 1 } => Other.ValidateMany(selectedScopedNodes, vc, state),

                // Otherwise we have too many results for a non-group validatable.
                _ => new ResultReport(ValidationResult.Failure,
                        new TraceAssertion(state.Location.InstanceLocation.ToString(), $"The FhirPath selector {Path} returned too many ({selected.Count}) results."))
            };

            static void initializeFhirPathCache(ValidationSettings vc, ValidationState state)
            {
                if (state.Global.FPCompilerCache is null)
                {
                    // use the compiler from the context, or otherwise the compiler with the FHIR dialect
                    var compiler = vc.FhirPathCompiler ?? new FhirPathCompiler(new SymbolTable().AddStandardFP().AddFhirExtensions());
                    state.Global.FPCompilerCache = new FhirPathCompilerCache(compiler);
                }
            }

            List<IScopedNode> cast(List<ITypedElement> elements) => elements.Select(e => e.AsScopedNode()).ToList();
        }

        /// <inheritdoc/>
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
