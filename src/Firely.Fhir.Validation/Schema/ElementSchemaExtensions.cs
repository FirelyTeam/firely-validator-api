/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    public static class ElementSchemaExtensions
    {
        public static bool IsEmpty(this ElementSchema elementSchema)
            => !elementSchema.Members.Any();

        public static ElementSchema WithMembers(this ElementSchema elementSchema, IEnumerable<IAssertion> additional) =>
             new(elementSchema.Id, elementSchema.Members.Union(additional));

        public static ElementSchema WithMembers(this ElementSchema elementSchema, params IAssertion[] additional)
            => elementSchema.WithMembers(additional.AsEnumerable());

        public static ElementSchema WithId(this ElementSchema elementSchema, Uri id) =>
            new(id, elementSchema.Members);

        public static ElementSchema WithId(this ElementSchema elementSchema, string id) =>
            new(id, elementSchema.Members);
    }
}
