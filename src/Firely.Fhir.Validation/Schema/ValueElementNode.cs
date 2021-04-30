/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Language;
using Hl7.Fhir.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    internal class ValueElementNode : BaseTypedElement
    {
        public ValueElementNode(ITypedElement wrapped) : base(wrapped)
        {
        }

        public override string Name => "value";

        public override string InstanceType => TypeSpecifier.ForNativeType(Wrapped.Value.GetType()).FullName;

        public override string Location => $"{Wrapped.Location}.value";

        public override IEnumerable<ITypedElement> Children(string? name = null) => Enumerable.Empty<ITypedElement>();
    }
}
