/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */
using System.Collections.Generic;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// 
    /// </summary>
    internal static class IAssertionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assertions"></param>
        /// <returns></returns>
        public static IAssertion GroupAll(this IEnumerable<IAssertion> assertions)
        {
            // No use having simple SUCCESS Results in an all, so we can optimize.
            var optimizedList = assertions.Where(a => a != ResultAssertion.SUCCESS).ToList();

            return optimizedList switch
            {
                { Count: 0 } => ResultAssertion.SUCCESS,
                { Count: 1 } list => list.Single(),
                var list => new AllValidator(list, shortcircuitEvaluation: true)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assertions"></param>
        /// <param name="emptyAssertion"></param>
        /// <returns></returns>
        public static IAssertion GroupAny(this IEnumerable<IAssertion> assertions, IAssertion? emptyAssertion = null)
        {
            var listOfAssertions = assertions.ToList();

            return listOfAssertions switch
            {
                { Count: 0 } => emptyAssertion ?? ResultAssertion.SUCCESS,
                { Count: 1 } list => list.Single(),
                var list when list.Any(a => a.IsAlways(ValidationResult.Success)) => ResultAssertion.SUCCESS,
                var list => new AnyValidator(list)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assertions"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static List<IAssertion> MaybeAdd(this List<IAssertion> assertions, IAssertion? element)
        {
            if (element is not null)
                assertions.Add(element);

            return assertions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="assertions"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static List<IAssertion> MaybeAddMany(this List<IAssertion> assertions, IEnumerable<IAssertion> element)
        {
            assertions.AddRange(element);
            return assertions;
        }


    }
}
