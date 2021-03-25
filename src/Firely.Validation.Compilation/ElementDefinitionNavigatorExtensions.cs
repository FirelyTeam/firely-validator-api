/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Firely.Validation.Compilation
{
    internal static class ElementDefinitionNavigatorExtensions
    {
        internal static bool IsResourcePlaceholder(this ElementDefinition ed)
            => ed.Type is not null && ed.Type.Any(t => t.Code == "Resource" || t.Code == "DomainResource");

        public static bool IsSlicing(this ElementDefinitionNavigator nav) => nav.Current.Slicing != null;

        /// <summary>
        /// Enumerate any succeeding direct child slices of the current slice intro.
        /// Skip any intermediate child elements and re-slice elements.
        /// When finished, return the navigator to the initial position.
        /// </summary>
        /// <param name="intro"></param>
        /// <returns>A sequence of <see cref="Bookmark"/> instances.</returns>
        internal static IEnumerable<Bookmark> FindMemberSlices(this ElementDefinitionNavigator intro)
        {
            if (!intro.IsSlicing()) throw new ArgumentException("Member slices can only be found relative to an intro slice.");

            var bm = intro.Bookmark();

            var pathName = intro.PathName;
            var introSliceName = intro.Current.SliceName;

            while (intro.MoveToNext(pathName))
            {
                var curName = intro.Current.SliceName;
                if (isResliceOf(curName, introSliceName))
                {
                    yield return intro.Bookmark();
                }

                bool isResliceOf(string parentSlice, string childSlice)
                {
                    if (parentSlice is null) return true;

                    return childSlice is not null &&
                        childSlice.StartsWith(parentSlice + '/');
                }
            }

            intro.ReturnToBookmark(bm);
        }
    }
}
