/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Utility;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Represents an informational assertion that has details about the StructureDefinition from which this schema is generated.
    /// </summary>
    [DataContract]
    internal record StructureDefinitionInformation : IJsonSerializable
    {
        /// <summary>
        /// How a type relates to its baseDefinition. (url: http://hl7.org/fhir/ValueSet/type-derivation-rule)
        /// </summary>
        /// <remarks>Maybe one day we can re-use the one in the R3/R4 specific FHIR libraries when they are moved to common.</remarks>
        [FhirEnumeration("TypeDerivationRule")]
        public enum TypeDerivationRule
        {
            /// <summary>
            /// This definition defines a new type that adds additional elements to the base type.
            /// </summary>
            [EnumLiteral("specialization", "http://hl7.org/fhir/type-derivation-rule")]
            [Hl7.Fhir.Utility.Description("Specialization")]
            Specialization,

            /// <summary>
            ///     This definition adds additional rules to an existing concrete type.
            /// </summary>
            [EnumLiteral("constraint", "http://hl7.org/fhir/type-derivation-rule")]
            [Hl7.Fhir.Utility.Description("Constraint")]
            Constraint
        }

        /// <summary>
        /// The canonical of the StructureDefinition from which this schema is derived.
        /// </summary>
        [DataMember]
        public Canonical Canonical { get; private set; }

        /// <summary>
        /// The list of canonicals for the StructureDefinitions from which this schema is generated.
        /// </summary>
        [DataMember]
        public Canonical[]? BaseCanonicals { get; private set; }

        /// <summary>
        /// The FHIR datatype of the StructureDefinitions from which this schema is generated.
        /// </summary>
        [DataMember]
        public string DataType { get; }

        /// <summary>
        /// specialization | constraint - How relates to base definition
        /// </summary>
        [DataMember]
        public TypeDerivationRule? Derivation { get; private set; }

        /// <summary>
        /// Whether the type represented in the StructureDefinition is abstract.
        /// </summary>
        [DataMember]
        public bool IsAbstract { get; private set; }

        /// <summary>
        /// Create an trace with a message and location.
        /// </summary>
        public StructureDefinitionInformation(Canonical canonical, Canonical[]? baseCanonicals, string dataType, TypeDerivationRule? derivation, bool isAbstract)
        {
            Canonical = canonical;
            BaseCanonicals = baseCanonicals;
            DataType = dataType;
            Derivation = derivation;
            IsAbstract = isAbstract;
        }

        /// <inheritdoc cref="IJsonSerializable.ToJson"/>
        public JToken ToJson()
        {
            var props = new List<JProperty> {
                new JProperty("url", Canonical.ToString()),
                new JProperty("base", BaseCanonicals is null ? null: string.Join(',', BaseCanonicals.Select(bc=>bc.ToString()))),
                new JProperty("datatype", DataType),
                new JProperty("abstract", IsAbstract)};

            if (Derivation is not null)
                props.Add(new JProperty("derivation", Derivation.GetLiteral()));

            return new JProperty("sd-info", new JObject(props));
        }
    }
}
