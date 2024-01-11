/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

#nullable disable

namespace Firely.Fhir.Validation.Compilation
{

    /// <summary>
    /// A generic TypeRefComponent based on the R4 ElementDefinition.TypeRefComponent
    /// </summary>
    internal class CommonTypeRefComponent
    {

#if STU3
        /// <summary>
        /// Converts a STU3 TypeRefComponent to this common TypeRefComponent
        /// </summary>
        /// <param name="typeRef">A STU3 TypeRefComponent</param>
        /// <returns>The common TypeRefComponent equivalent of <paramref name="typeRef"/> </returns>
        public static CommonTypeRefComponent Convert(ElementDefinition.TypeRefComponent typeRef)
            => new(
                typeRef.CodeElement,
                typeRef.Profile is not null ? new[] { new Hl7.Fhir.Model.Canonical(typeRef.Profile) } : null,
                typeRef.TargetProfile is not null ? new[] { new Hl7.Fhir.Model.Canonical(typeRef.TargetProfile) } : null,
                typeRef.AggregationElement,
                typeRef.VersioningElement);

        /// <summary>
        /// Checks whether it is possible to converts an enumeration of STU3 TypeRefComponents to the CommonTypeRefComponent
        /// </summary>
        /// <param name="typeRefs">An enumeration of STU3 TypeRefComponents</param>
        /// <returns><c>true</c> if an enumeration of STU3 TypeRefComponents can be converted; otherwise, <c>false</c>.</returns>
        public static bool CanConvert(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            var groups = typeRefs.GroupBy(t => t.Code);
            return groups.All(group => canConvert(group));

            IEnumerable<(string, string)> cartesianProduct(string[] left, string[] right) =>
               left.Join(right, x => true, y => true, (l, r) => (l, r));

            bool canConvert(IEnumerable<TypeRefComponent> typeRef)
            {
                var profiles = typeRef.Where(t => t.Profile is not null).Select(t => t.Profile).Distinct().ToArray();
                var targetProfiles = typeRef.Where(t => t.TargetProfile is not null).Select(t => t.TargetProfile).Distinct().ToArray();

                var product = cartesianProduct(profiles, targetProfiles);

                return product.Count() == typeRef.Count()
                    ? typeRef.Select(t => (t.Profile, t.TargetProfile)).All(a => product.Contains(a))
                    : profiles.Length == 0 || targetProfiles.Length == 0;
            }
        }

        /// <summary>
        /// Converts an enumeration of STU3 TypeRefComponents to this common TypeRefComponents
        /// </summary>
        /// <param name="typeRefs">An enumeration of STU3 TypeRefComponents</param>
        /// <returns>An enumeration of common TypeRefComponent equivalent of <paramref name="typeRefs"/></returns>
        public static IEnumerable<CommonTypeRefComponent> Convert(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            // convert typeRefs to a common format:
            foreach (var code in typeRefs.GroupBy(t => t.Code))
            {
                yield return new CommonTypeRefComponent(
                    addExtensions(code.Key, code.SelectMany(t => t.CodeElement.Extension)),
                    code.Where(t => t.Profile is not null).Select(t => new Hl7.Fhir.Model.Canonical(t.Profile)),
                    code.Where(t => t.TargetProfile is not null).Select(t => new Hl7.Fhir.Model.Canonical(t.TargetProfile)),
                    code.SelectMany(t => t.AggregationElement).Distinct(),
                    code.Select(t => t.VersioningElement).Distinct().SingleOrDefault());
            }

            static FhirUri addExtensions(string code, IEnumerable<Extension> extensions)
            {
                FhirUri result = new(code);
                foreach (var item in extensions)
                {
                    result.AddExtension(item.Url, item.Value);

                }
                return result;
            }
        }
#else
        /// <summary>
        /// Checks whether it is possible to converts an enumeration of R4 <see cref="TypeRefComponent"/>s to the <see cref="CommonTypeRefComponent"/>
        /// </summary>
        /// <param name="_">An enumeration of R4 <see cref="TypeRefComponent"/>s</param>
        /// <returns>This always true, because <see cref="CommonTypeRefComponent"/> has been derived from R4 <see cref="TypeRefComponent"/></returns>
        public static bool CanConvert(IEnumerable<ElementDefinition.TypeRefComponent> _) => true;

        public static CommonTypeRefComponent Convert(ElementDefinition.TypeRefComponent typeRef)
           => new(
               typeRef.CodeElement,
               typeRef.ProfileElement,
               typeRef.TargetProfileElement,
               typeRef.AggregationElement,
               typeRef.VersioningElement);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeRefs"></param>
        /// <returns></returns>
        public static IEnumerable<CommonTypeRefComponent> Convert(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs) =>
            typeRefs.Select(t => new CommonTypeRefComponent(t.CodeElement, t.ProfileElement, t.TargetProfileElement, t.AggregationElement, t.VersioningElement));
#endif

        /// <summary>
        /// Initialize a new instance of CommonTypeRefComponent class
        /// </summary>
        /// <param name="codeElement"></param>
        /// <param name="profileElement"></param>
        /// <param name="targetProfileElement"></param>
        /// <param name="aggregationElement"></param>
        /// <param name="versioningElement"></param>
        public CommonTypeRefComponent(
            FhirUri codeElement,
            IEnumerable<Hl7.Fhir.Model.Canonical> profileElement,
            IEnumerable<Hl7.Fhir.Model.Canonical> targetProfileElement,
            IEnumerable<Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>> aggregationElement,
            Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules> versioningElement)
        {

            if (codeElement != null) CodeElement = (Hl7.Fhir.Model.FhirUri)codeElement.DeepCopy();
            if (profileElement?.Any() == true) ProfileElement = new List<Hl7.Fhir.Model.Canonical>(profileElement.DeepCopy());
            if (targetProfileElement?.Any() == true) TargetProfileElement = new List<Hl7.Fhir.Model.Canonical>(targetProfileElement.DeepCopy());
            if (aggregationElement?.Any() == true) AggregationElement = new List<Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>>(aggregationElement.DeepCopy());
            if (versioningElement != null) VersioningElement = (Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules>)versioningElement.DeepCopy();
        }

        /// <summary>
        /// Initialize a new instance of CommonTypeRefComponent class without parameters
        /// </summary>
        internal CommonTypeRefComponent()
        { }

        /// <summary>
        /// Data type or Resource (reference to definition)
        /// </summary>
        public Hl7.Fhir.Model.FhirUri CodeElement
        {
            get { return _codeElement; }
            set { _codeElement = value; }
        }

        private Hl7.Fhir.Model.FhirUri _codeElement;

        /// <summary>
        /// Data type or Resource (reference to definition)
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public string Code
        {
            get { return CodeElement?.Value; }
            set
            {
                CodeElement = value is null ? null : new Hl7.Fhir.Model.FhirUri(value);
            }
        }

        /// <summary>
        /// Profiles (StructureDefinition or IG) - one must apply
        /// </summary>
        public List<Hl7.Fhir.Model.Canonical> ProfileElement
        {
            get { _profileElement ??= new List<Hl7.Fhir.Model.Canonical>(); return _profileElement; }
            set { _profileElement = value; }
        }

        private List<Hl7.Fhir.Model.Canonical> _profileElement;

        /// <summary>
        /// Profiles (StructureDefinition or IG) - one must apply
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public IEnumerable<string> Profile
        {
            get { return ProfileElement?.Select(elem => elem.Value); }
            set
            {
                ProfileElement = value is null ? null : new List<Hl7.Fhir.Model.Canonical>(value.Select(elem => new Hl7.Fhir.Model.Canonical(elem)));
            }
        }

        /// <summary>
        /// Profile (StructureDefinition or IG) on the Reference/canonical target - one must apply
        /// </summary>
        public List<Hl7.Fhir.Model.Canonical> TargetProfileElement
        {
            get { _targetProfileElement ??= new List<Hl7.Fhir.Model.Canonical>(); return _targetProfileElement; }
            set { _targetProfileElement = value; }
        }

        private List<Hl7.Fhir.Model.Canonical> _targetProfileElement;

        /// <summary>
        /// Profile (StructureDefinition or IG) on the Reference/canonical target - one must apply
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public IEnumerable<string> TargetProfile
        {
            get { return TargetProfileElement?.Select(elem => elem.Value); }
            set
            {
                TargetProfileElement = value is null ? null : new List<Hl7.Fhir.Model.Canonical>(value.Select(elem => new Hl7.Fhir.Model.Canonical(elem)));
            }
        }

        /// <summary>
        /// contained | referenced | bundled - how aggregated
        /// </summary>
        public List<Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>> AggregationElement
        {
            get { _aggregationElement ??= new List<Hl7.Fhir.Model.Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>>(); return _aggregationElement; }
            set { _aggregationElement = value; }
        }

        private List<Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>> _aggregationElement;

        /// <summary>
        /// contained | referenced | bundled - how aggregated
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public IEnumerable<Hl7.Fhir.Model.ElementDefinition.AggregationMode?> Aggregation
        {
            get { return AggregationElement?.Select(elem => elem.Value); }
            set
            {
                AggregationElement = value is null
                    ? null
                    : new List<Hl7.Fhir.Model.Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>>(value.Select(elem => new Hl7.Fhir.Model.Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>(elem)));
            }
        }

        /// <summary>
        /// either | independent | specific
        /// </summary>
        public Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules> VersioningElement
        {
            get { return _versioningElement; }
            set { _versioningElement = value; }
        }

        private Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules> _versioningElement;

        /// <summary>
        /// either | independent | specific
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules? Versioning
        {
            get { return VersioningElement?.Value; }
            set
            {
                VersioningElement = value is null ? null : new Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules>(value);
            }
        }

    }
}
#nullable restore