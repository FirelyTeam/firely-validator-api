/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using System;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// The arguments passed to event handlers subscribed to <see cref="ValidationContext.OnExternalResolutionNeeded"/>.
    /// </summary>
    public class OnResolveResourceReferenceEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs a minimal argument for the event.
        /// </summary>
        /// <param name="reference"></param>
        public OnResolveResourceReferenceEventArgs(string reference)
        {
            Reference = reference;
        }

        /// <summary>
        /// The reference to be resolved, as set by the constructor.
        /// </summary>
        public string Reference { get; }

        /// <summary>
        /// The result of the resolution, as set by the called event.
        /// </summary>
        public ITypedElement? Result { get; set; }
    }
}
