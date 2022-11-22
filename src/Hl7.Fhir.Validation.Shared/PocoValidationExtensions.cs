﻿/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using System.Collections.Generic;

namespace Hl7.Fhir.Validation
{
    /// <summary>
    /// Add support for validating against Base subclasses (instead of ITypedElement) to the Validator
    /// </summary>
    public static class PocoValidationExtensions
    {
        /// <summary>
        /// Validate an instance, use the instance's type to pick the relevant profile to validate against.
        /// </summary>
        public static OperationOutcome Validate(this Validator me, Base instance)
        {
            return me.Validate(instance.ToTypedElement());
        }

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public static OperationOutcome Validate(this Validator me, Base instance, params string[] definitionUri)
        {
            return me.Validate(instance.ToTypedElement(), definitionUri);
        }

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public static OperationOutcome Validate(this Validator me, Base instance, StructureDefinition structureDefinition)
        {
            return me.Validate(instance.ToTypedElement(), structureDefinition);
        }

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public static OperationOutcome Validate(this Validator me, Base instance, IEnumerable<StructureDefinition> structureDefinitions)
        {
            return me.Validate(instance.ToTypedElement(), structureDefinitions);
        }
    }
}
