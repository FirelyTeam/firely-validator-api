/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Asserts the order of elements when the data originated from XML.
    /// </summary>
    [DataContract]
    public class XmlOrderValidator : BasicValidator
    {
        /// <summary>
        /// The relative order of the child represented by this schema in comparison
        /// to other children.
        /// </summary>
        [DataMember]
        public int Order { get; private set; }

        /// <summary>
        /// Initializes a new XmlOrderValidator given its relative order.
        /// </summary>
        public XmlOrderValidator(int order)
        {
            Order = order;
        }

        /// <inheritdoc/>
        public override string Key => "xml-order";

        /// <inheritdoc/>
        public override object Value => Order;

        /// <inheritdoc/>
        public override ResultAssertion Validate(ITypedElement input, ValidationContext _, ValidationState __)
            => ResultAssertion.SUCCESS;
    }
}
