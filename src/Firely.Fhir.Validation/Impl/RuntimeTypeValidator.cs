/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Support;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the validity of an element against the constraints for its runtime type,
    /// as indicated in <see cref="ITypedElement.InstanceType"/>.
    /// </summary>
    [DataContract]
    public class RuntimeTypeValidator : IValidatable
    {
        /// <inheritdoc />
        public ResultReport Validate(ITypedElement input, ValidationContext vc, ValidationState vs)
        {
            if (input.InstanceType is null)
            {
                return new ResultReport(ValidationResult.Undecided, new IssueAssertion(Issue.CONTENT_ELEMENT_CANNOT_DETERMINE_TYPE,
                    input.Location, $"The type of the element is unknown, so it cannot be validated against its type only."));
            }

            var schemaUri = MapTypeNameToFhirStructureDefinitionSchema(input.InstanceType);

            // Validate the instance against the uri using a SchemaReferenceValidator
            var schemaValidatorInternal = new SchemaReferenceValidator(schemaUri);
            return schemaValidatorInternal.ValidateOne(input, vc, vs);
        }

        /// <summary>
        /// Turn a datatype name into a schema uri.
        /// </summary>
        /// <remarks>Note how this ties the data type names strictly to a HL7-defined url for
        /// the schema's.</remarks>
        public static Canonical MapTypeNameToFhirStructureDefinitionSchema(string typeName)
        {
            var typeNameUri = new Canonical(typeName);

            return typeNameUri.IsAbsolute ? typeNameUri : ResourceIdentity.Core(typeName).OriginalString;
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson() => new JProperty("typeschema", "(from InstanceType)");
    }
}
