using Hl7.Fhir.Model;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace Firely.Fhir.Validation.Compilation
{

    /// <summary>
    /// 
    /// </summary>
    internal class CommonTypeRefComponent
    {

#if STU3
        public static CommonTypeRefComponent Convert(ElementDefinition.TypeRefComponent typeRef)
            => new(
                typeRef.CodeElement,
                typeRef.Profile is not null ? new[] { new Hl7.Fhir.Model.Canonical(typeRef.Profile) } : null,
                typeRef.TargetProfile is not null ? new[] { new Hl7.Fhir.Model.Canonical(typeRef.TargetProfile) } : null,
                typeRef.AggregationElement,
                typeRef.VersioningElement);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeRefs"></param>
        /// <returns></returns>
        public static IEnumerable<CommonTypeRefComponent> Convert(IEnumerable<ElementDefinition.TypeRefComponent> typeRefs)
        {
            // convert typeRefs to a R4 format:
            foreach (var code in typeRefs.GroupBy(t => t.Code))
            {
                yield return new CommonTypeRefComponent(
                    foo(code.Key, code.SelectMany(t => t.CodeElement.Extension)),
                    code.Where(t => t.Profile is not null).Select(t => new Hl7.Fhir.Model.Canonical(t.Profile)),
                    code.Where(t => t.TargetProfile is not null).Select(t => new Hl7.Fhir.Model.Canonical(t.TargetProfile)),
                    code.SelectMany(t => t.AggregationElement).Distinct(),
                    code.Select(t => t.VersioningElement).Distinct().SingleOrDefault());
            }

            FhirUri foo(string code, IEnumerable<Extension> extensions)
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
        /// 
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
        /// Data type or Resource (reference to definition)
        /// </summary>
        public Hl7.Fhir.Model.FhirUri CodeElement
        {
            get { return _CodeElement; }
            set { _CodeElement = value; }
        }

        private Hl7.Fhir.Model.FhirUri _CodeElement;

        /// <summary>
        /// Data type or Resource (reference to definition)
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public string Code
        {
            get { return CodeElement != null ? CodeElement.Value : null; }
            set
            {
                if (value == null)
                    CodeElement = null;
                else
                    CodeElement = new Hl7.Fhir.Model.FhirUri(value);
            }
        }

        /// <summary>
        /// Profiles (StructureDefinition or IG) - one must apply
        /// </summary>
        public List<Hl7.Fhir.Model.Canonical> ProfileElement
        {
            get { if (_ProfileElement == null) _ProfileElement = new List<Hl7.Fhir.Model.Canonical>(); return _ProfileElement; }
            set { _ProfileElement = value; }
        }

        private List<Hl7.Fhir.Model.Canonical> _ProfileElement;

        /// <summary>
        /// Profiles (StructureDefinition or IG) - one must apply
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public IEnumerable<string> Profile
        {
            get { return ProfileElement != null ? ProfileElement.Select(elem => elem.Value) : null; }
            set
            {
                if (value == null)
                    ProfileElement = null;
                else
                    ProfileElement = new List<Hl7.Fhir.Model.Canonical>(value.Select(elem => new Hl7.Fhir.Model.Canonical(elem)));
            }
        }

        /// <summary>
        /// Profile (StructureDefinition or IG) on the Reference/canonical target - one must apply
        /// </summary>
        public List<Hl7.Fhir.Model.Canonical> TargetProfileElement
        {
            get { if (_TargetProfileElement == null) _TargetProfileElement = new List<Hl7.Fhir.Model.Canonical>(); return _TargetProfileElement; }
            set { _TargetProfileElement = value; }
        }

        private List<Hl7.Fhir.Model.Canonical> _TargetProfileElement;

        /// <summary>
        /// Profile (StructureDefinition or IG) on the Reference/canonical target - one must apply
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public IEnumerable<string> TargetProfile
        {
            get { return TargetProfileElement != null ? TargetProfileElement.Select(elem => elem.Value) : null; }
            set
            {
                if (value == null)
                    TargetProfileElement = null;
                else
                    TargetProfileElement = new List<Hl7.Fhir.Model.Canonical>(value.Select(elem => new Hl7.Fhir.Model.Canonical(elem)));
            }
        }

        /// <summary>
        /// contained | referenced | bundled - how aggregated
        /// </summary>
        public List<Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>> AggregationElement
        {
            get { if (_AggregationElement == null) _AggregationElement = new List<Hl7.Fhir.Model.Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>>(); return _AggregationElement; }
            set { _AggregationElement = value; }
        }

        private List<Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>> _AggregationElement;

        /// <summary>
        /// contained | referenced | bundled - how aggregated
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public IEnumerable<Hl7.Fhir.Model.ElementDefinition.AggregationMode?> Aggregation
        {
            get { return AggregationElement != null ? AggregationElement.Select(elem => elem.Value) : null; }
            set
            {
                if (value == null)
                    AggregationElement = null;
                else
                    AggregationElement = new List<Hl7.Fhir.Model.Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>>(value.Select(elem => new Hl7.Fhir.Model.Code<Hl7.Fhir.Model.ElementDefinition.AggregationMode>(elem)));
            }
        }

        /// <summary>
        /// either | independent | specific
        /// </summary>
        public Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules> VersioningElement
        {
            get { return _VersioningElement; }
            set { _VersioningElement = value; }
        }

        private Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules> _VersioningElement;

        /// <summary>
        /// either | independent | specific
        /// </summary>
        /// <remarks>This uses the native .NET datatype, rather than the FHIR equivalent</remarks>
        public Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules? Versioning
        {
            get { return VersioningElement != null ? VersioningElement.Value : null; }
            set
            {
                if (value == null)
                    VersioningElement = null;
                else
                    VersioningElement = new Code<Hl7.Fhir.Model.ElementDefinition.ReferenceVersionRules>(value);
            }
        }

    }
}
#nullable restore