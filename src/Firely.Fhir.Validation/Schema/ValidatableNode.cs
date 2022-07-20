/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */


#nullable enable

namespace Firely.Fhir.Validation
{
    // TODO: It is generally useful to have the source uri from which the resource under
    // consideration was retrieved - so we might want to move 'ResourceUrl' into ScopedNode.
    // This wrapper can then be removed.
    //internal class ValidatableNode : ScopedNode
    //{
    //    public ValidatableNode(ITypedElement wrapped, string? resourceUrl) : base(unwrap(wrapped))
    //    {
    //        ResourceUrl = resourceUrl;
    //    }

    //    public string? ResourceUrl { get; }

    //    private static ITypedElement unwrap(ITypedElement te) => te.GetType() == typeof(ScopedNode) ? unwrap(((ScopedNode)te).Current) : te;
    //}
}
