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
    //TODO: After the April 2021 version of the SDK is published, this code can be removed and replaced
    //by the equivalent functions in the SDK.
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
        public static IEnumerable<Bookmark> FindMemberSlices(this ElementDefinitionNavigator intro)
        {
            if (!intro.IsSlicing()) throw new ArgumentException("Member slices can only be found relative to an intro slice.");

            var bm = intro.Bookmark();

            var pathName = intro.PathName;
            var introSliceName = intro.Current.SliceName;

            while (intro.MoveToNext(pathName))
            {
                var currentSliceName = intro.Current.SliceName;
                if (IsDirectSliceOf(currentSliceName, introSliceName))
                {
                    yield return intro.Bookmark();
                }
            }

            intro.ReturnToBookmark(bm);
        }

        /// <summary>
        /// This detects whether a child slice name indicates the slicing is a one-level deeper (re)slicing
        /// of the parent slice (or of an unsliced element):
        /// 
        /// IsResliceOf(null,null) == false, IsResliceOf("A",null) == true, IsResliceOf("A/B", null) == false,
        /// IsResliceOf("A", "A") == false, IsResliceOf("B","A") == false, 
        /// IsResliceOf("A/B","A") == true, IsResliceof("A/B/C", "A") == false, 
        /// IsResliceOf("B/C", "A") == false, IsResliceOf("AA", "A") is false, IsResliceOf("A/BB", "A/B") == false
        /// </summary>
        /// <param name="parentSliceName"></param>
        /// <param name="childSliceName"></param>
        /// <returns></returns>
        public static bool IsDirectSliceOf(string childSliceName, string parentSliceName)
        {
            if (childSliceName is null) return false;

            var prefix = parentSliceName is null ? "" : parentSliceName + "/";
            if (!childSliceName.StartsWith(prefix)) return false;

            return !childSliceName[prefix.Length..].Contains("/");
        }
    }
}
