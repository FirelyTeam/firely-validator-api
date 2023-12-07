/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Function that maps a local type name (as found on e.g. <c>ElementDefinition.TypeRef.Code</c> to
    /// to a resolvable url.
    /// </summary>
    /// <returns>May return null if the local name has no mapping to a resolvable canonical url.</returns>
    public delegate Canonical? TypeNameMapper(string local);

    /// <summary>
    /// Extension methods for use on top of <see cref="TypeNameMapper"/>.
    /// </summary>
    internal static class TypeNameMapperExtensions
    {
        /// <summary>
        /// Will invoke the <see cref="TypeNameMapper"/>, if set. If there is no mapper, or the mapper returns
        /// <c>null</c>, <see cref="Canonical.ForCoreType(string)"/> is used instead.
        /// </summary>
        public static Canonical MapTypeName(this TypeNameMapper? mapper, string local) =>
           mapper switch
           {
               TypeNameMapper m => m(local) ?? Canonical.ForCoreType(local),
               _ => Canonical.ForCoreType(local)
           };
    }
}
