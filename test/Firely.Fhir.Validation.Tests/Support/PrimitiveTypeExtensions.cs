/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// This class is just temporarily and should be replaced when the new Typesystem has been implemented
    /// https://gist.github.com/ewoutkramer/30fbb9b62c4f493dc479129f80ad0e23
    /// </summary>
    internal static class PrimitiveTypeExtensions
    {
        public static ITypedElement ToTypedElement(this PrimitiveType primitiveType)
            => ElementNodeAdapter.Root(primitiveType.TypeName, name: primitiveType.GetType().Name, value: primitiveType.ObjectValue);

        public static ITypedElement ToTypedElement<T, V>(V value) where T : PrimitiveType, IValue<V>, new()
        {
            var instance = new T
            {
                ObjectValue = value
            };
            return instance.ToTypedElement();
        }
    }
}
