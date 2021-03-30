/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A read-only collection of <see cref="IAssertion"/>.
    /// </summary>
    public class Assertions : ReadOnlyCollection<IAssertion>
    {
        /// <summary>
        /// A collection with a single <see cref="ResultAssertion"> signaling that the instance was valid.</see>/>
        /// </summary>
        public static readonly Assertions SUCCESS = new Assertions(ResultAssertion.SUCCESS);

        /// <summary>
        /// A collection with a single <see cref="ResultAssertion"> signaling that the instance failed validation.</see>/>
        /// </summary>
        public static readonly Assertions FAILURE = new Assertions(ResultAssertion.FAILURE);

        /// <summary>
        /// A collection with a single <see cref="ResultAssertion"> signaling that the validation result is undecided.</see>/>
        /// </summary>
        public static readonly Assertions UNDECIDED = new Assertions(ResultAssertion.UNDECIDED);

        /// <summary>
        /// An empty collection.
        /// </summary>
        public static readonly Assertions EMPTY = new Assertions();

        /// <inheritdoc cref="Assertions(IEnumerable{IAssertion})"/>
        public Assertions(params IAssertion[] assertions) : this(assertions.AsEnumerable())
        {
        }

        /// <summary>
        /// Initializes a new list of assertions with an existing list of assertions.
        /// </summary>
        /// <remarks>Assertions in <paramref name="assertions"/> of the same type will be merged in the
        /// newly created list.
        /// </remarks>
        /// <param name="assertions"></param>
        public Assertions(IEnumerable<IAssertion>? assertions) : base(merge(assertions ?? Assertions.EMPTY).ToList())
        {
        }


        /// <summary>
        /// Returns the union of two <see cref="Assertions"/>, duplicates are removed
        /// and compatible assertions are merged.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Assertions operator +(Assertions left, Assertions right)
            => new Assertions(left.Union(right));

        /// <summary>
        /// Appends a single <see cref="IAssertion"/> to an <see cref="Assertions"/>, duplicates are removed
        /// and compatible assertions are merged.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Assertions operator +(Assertions left, IAssertion right)
                => new Assertions(left.Union(new[] { right }));

        private static IEnumerable<IAssertion> merge(IEnumerable<IAssertion> assertions)
        {
            var mergeable = assertions.OfType<IMergeable>();
            var nonMergeable = assertions.Where(a => !(a is IMergeable));

            var merged =
                from sa in mergeable
                group sa by sa.GetType() into grp
                select (IAssertion)grp.Aggregate((sum, other) => sum.Merge(other));

            return nonMergeable.Union(merged);
        }

        /// <summary>
        /// Returns the <see cref="ResultAssertion"/> in the list of assertions.
        /// </summary>
        public ResultAssertion Result => this.OfType<ResultAssertion>().SingleOrDefault() ?? ResultAssertion.UNDECIDED;
    }


}
