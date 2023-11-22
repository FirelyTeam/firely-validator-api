/* 
 * Copyright (C) 2023, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.FhirPath;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// A FHIR profile validator, which is able to validate a FHIR resource against a given FHIR profile.
    /// </summary>
    public class Validator
    {
        /// <summary>
        /// Constructs a validator, passing in the services the validator depends on.
        /// </summary>
        /// <param name="terminologyService">An <see cref="ITerminologyService"/> that is used when the validator must validate a code against a 
        /// terminology service.</param>
        /// <param name="resourceResolver">An <see cref="IResourceResolver"/> that is used to resolve the StructureDefinitions to validate against.</param>
        /// <param name="referenceResolver">A <see cref="IExternalReferenceResolver"/> that resolves an url to an external instance, represented as a Model POCO.</param>
        /// <param name="fhirPathCompiler">Optionally, a FhirPath compiler to use when evaluating constraints
        /// (provide this if you have custom functions included in the symbol table).</param>
        public Validator(
            ICodeValidationTerminologyService terminologyService,
            IAsyncResourceResolver resourceResolver,
            IExternalReferenceResolver referenceResolver,
            FhirPathCompiler? fhirPathCompiler = null)
        {
            var elementSchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(resourceResolver);

            _validationContext = new ValidationContext(elementSchemaResolver, terminologyService)
            {
                FhirPathCompiler = fhirPathCompiler,
                ExternalReferenceResolver = resolve,
                ConstraintBestPractices = ConstraintBestPractices,
                MetaProfileSelector = MetaProfileSelector,
                ExtensionUrlFollower = ExtensionUrlFollower
            };

            async Task<ITypedElement?> resolve(string reference)
            {
                var r = await referenceResolver.ResolveAsync(reference);
                return r?.ToTypedElement(ModelInfo.ModelInspector);
            }
        }

        private readonly ValidationContext _validationContext;

        /// <summary>
        /// Determines how to deal with failures of FhirPath constraints marked as "best practice". Default is <see cref="ValidateBestPracticesSeverity.Warning"/>.
        /// </summary>
        /// <remarks>See <see cref="FhirPathValidator.BestPractice"/>, <see cref="ValidateBestPracticesSeverity"/> and
        /// https://www.hl7.org/fhir/best-practices.html for more information.</remarks>
        public ValidateBestPracticesSeverity ConstraintBestPractices = ValidateBestPracticesSeverity.Warning;

        /// <summary>
        /// The <see cref="MetaProfileSelector"/> to invoke when a <see cref="Meta.Profile"/> is encountered. If not set, the list of profiles
        /// is used as encountered in the instance.
        /// </summary>
        public MetaProfileSelector? MetaProfileSelector = null;

        /// <summary>
        /// The <see cref="Validation.ExtensionUrlFollower"/> to invoke when an <see cref="Extension"/> is encountered in an instance.
        /// If not set, then a validation of an Extension will warn if the extension cannot be resolved, or will return an error when 
        /// the extension cannot be resolved and is a modififier extension.
        /// </summary>
        public ExtensionUrlFollower? ExtensionUrlFollower = null;

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
        public ResultReport Validate(Resource instance, string profile)
        {
            var validator = new SchemaReferenceValidator(profile);
            return validator.Validate(instance.ToTypedElement(ModelInfo.ModelInspector).AsScopedNode(), _validationContext);
        }
    }
}
