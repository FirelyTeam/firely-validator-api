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
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using System;

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
        /// <param name="resourceResolver">An <see cref="IResourceResolver"/> that is used to resolve the StructureDefinitions to validate against.</param>
        /// <param name="terminologyService">An <see cref="ITerminologyService"/> that is used when the validator must validate a code against a 
        /// terminology service.</param>
        /// <param name="referenceResolver">A <see cref="IExternalReferenceResolver"/> that resolves an url to an external instance, represented as a Model POCO.</param>
        /// <param name="fhirPathCompiler">Optionally, a FhirPath compiler to use when evaluating constraints
        /// (provide this if you have custom functions included in the symbol table).</param>
        public Validator(
            IAsyncResourceResolver resourceResolver,
            ICodeValidationTerminologyService terminologyService,
            IExternalReferenceResolver? referenceResolver = null,
            FhirPathCompiler? fhirPathCompiler = null)
        {
            _elementSchemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(resourceResolver);

            TerminologyService = terminologyService;
            ResolveExternalReference = referenceResolver;
            FhirPathCompiler = fhirPathCompiler;
        }

        private ValidationContext buildContext()
        {
            var validationContext = new ValidationContext(_elementSchemaResolver, TerminologyService)
            {
                FhirPathCompiler = FhirPathCompiler,
                ResolveExternalReference = ResolveExternalReference is not null ? resolve : null,
                ConstraintBestPractices = ConstraintBestPractices,
                SelectMetaProfiles = SelectMetaProfiles,
                FollowExtensionUrl = FollowExtensionUrl,
                TypeNameMapper = TypeNameMapper
            };

            if (SkipConstraintValidation)
                validationContext.ExcludeFilters.Add(ass => ass is FhirPathValidator);

            return validationContext;

            ITypedElement? resolve(string reference, string location)
            {
                var r = TaskHelper.Await(() => ResolveExternalReference.ResolveAsync(reference));
                return toTypedElement(r);
            }
        }

        private static ITypedElement? toTypedElement(object? o) =>
            o switch
            {
                null => null,
                ElementNode en => en,
                Resource r => r.ToTypedElement(ModelInfo.ModelInspector),
                _ => throw new ArgumentException("Reference resolver must return either a Resource or ElementNode.")
            };

        private readonly IElementSchemaResolver _elementSchemaResolver;

        /// <summary>
        /// An <see cref="ITerminologyService"/> that is used when the validator must validate a code against a 
        /// terminology service.
        /// </summary>
        public ICodeValidationTerminologyService TerminologyService;


        /// <summary>
        /// A <see cref="IExternalReferenceResolver"/> that resolves an url to an external instance, represented as a Model POCO.
        /// </summary>
        public IExternalReferenceResolver? ResolveExternalReference = null;

        /// <summary>
        /// Optionally, a FhirPath compiler to use when evaluating constraints.
        /// </summary>
        /// <remarks>Provide this if you have custom functions included in the symbol table.</remarks>
        public FhirPathCompiler? FhirPathCompiler = null;

        /// <summary>
        /// Determines how to deal with failures of FhirPath constraints marked as "best practice". Default is <see cref="ValidateBestPracticesSeverity.Warning"/>.
        /// </summary>
        /// <remarks>See <see cref="FhirPathValidator.BestPractice"/>, <see cref="ValidateBestPracticesSeverity"/> and
        /// https://www.hl7.org/fhir/best-practices.html for more information.</remarks>
        public ValidateBestPracticesSeverity ConstraintBestPractices = ValidateBestPracticesSeverity.Warning;

        /// <summary>
        /// The <see cref="SelectMetaProfiles"/> to invoke when a <see cref="Meta.Profile"/> is encountered. If not set, the list of profiles
        /// is used as encountered in the instance.
        /// </summary>
        public MetaProfileSelector? SelectMetaProfiles = null;

        /// <summary>
        /// The <see cref="Validation.ExtensionUrlFollower"/> to invoke when an <see cref="Extension"/> is encountered in an instance.
        /// If not set, then a validation of an Extension will warn if the extension cannot be resolved, or will return an error when 
        /// the extension cannot be resolved and is a modififier extension.
        /// </summary>
        public ExtensionUrlFollower? FollowExtensionUrl = null;

        /// <summary>
        /// A function that maps a type name found in <c>TypeRefComponent.Code</c> to a resolvable canonical.
        /// If not set, it will prefix the type with the standard <c>http://hl7.org/fhir/StructureDefinition</c> prefix.
        /// </summary>
        public TypeNameMapper? TypeNameMapper = null;

        /// <summary>
        /// StructureDefinition may contain FhirPath constraints to enfore invariants in the data that cannot
        /// be expresses using StructureDefinition alone. This validation can be turned off for performance or
        /// debugging purposes. Default is 'false'.
        /// </summary>
        public bool SkipConstraintValidation = false;

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        // Suppressing this issue since this will be a single method call when we introduce IScopedNode.
        public ResultReport Validate(Resource instance, string? profile = null) => Validate(instance.ToTypedElement(ModelInfo.ModelInspector).AsScopedNode(), profile);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        /// <summary>
        /// Validates an instance against a profile.
        /// </summary>
        /// <returns>A report containing the issues found during validation.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public ResultReport Validate(ElementNode instance, string? profile = null) => Validate(instance.AsScopedNode(), profile);
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters

        internal ResultReport Validate(IScopedNode sn, string? profile = null)
        {
            profile ??= Canonical.ForCoreType(sn.InstanceType).ToString();

            var validator = new SchemaReferenceValidator(profile);
            var ctx = buildContext();
            return validator.Validate(sn, ctx);
        }
    }
}
