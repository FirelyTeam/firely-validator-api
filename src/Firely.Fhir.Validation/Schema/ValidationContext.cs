﻿/* 
 * Copyright (c) 2019, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/fhir-net-api/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.ElementModel.Types;
using Hl7.FhirPath;
using System;
using System.Threading.Tasks;

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// Dependencies and settings used by the validator across all invocations.
    /// </summary>
    /// <remarks>Generally, you will configure one such <see cref="ValidationContext"/> within a 
    /// subsystem doing validation.</remarks>
    public class ValidationContext
    {
        public ValidationContext(IElementSchemaResolver schemaResolver, ITerminologyServiceNEW terminologyService)
        {
            ElementSchemaResolver = schemaResolver ?? throw new ArgumentNullException(nameof(schemaResolver));
            TerminologyService = terminologyService ?? throw new ArgumentNullException(nameof(terminologyService));
        }

        public ValidationContext(IElementSchemaResolver schemaResolver, IValidateCodeService validateCodeService)
        {
            ElementSchemaResolver = schemaResolver ?? throw new ArgumentNullException(nameof(schemaResolver));
            ValidateCodeService = validateCodeService ?? throw new ArgumentNullException(nameof(validateCodeService));
        }

        /// <summary>
        /// The terminology service to use. One of <see cref="TerminologyService"/> or <see cref="ValidateCodeService"/> must be set.
        /// </summary>
        public ITerminologyServiceNEW? TerminologyService;

        /// <summary>
        /// The terminology service to use. One of <see cref="TerminologyService"/> or <see cref="ValidateCodeService"/> must be set.
        /// </summary>
        public IValidateCodeService? ValidateCodeService;

        /// <summary>
        /// An <see cref="IElementSchemaResolver"/> that is used when the validator encounters a reference to
        /// another schema.
        /// </summary>
        /// <remarks>Note that this is not just for "external" schemas (e.g. those referred to by Extension.url). The much more common
        /// case is where the schema for a resource references the schema for the type of one of its elements.
        /// </remarks>
        public IElementSchemaResolver ElementSchemaResolver;

        /// <summary>
        /// A function that resolves an url to an external instance, parsed as an <see cref="ITypedElement"/>.
        /// </summary>
        /// <remarks>FHIR instances can refer to other instances using types like canonical or a FHIR Reference.
        /// If this property is set, the validator will try to fetch such resources and validate them. Note that
        /// this property is only relevant for references referring to another "external" instance, references 
        /// to contained resources will always be followed. If this property is not set, references will be 
        /// ignored.
        /// </remarks>
        public Func<string, Task<ITypedElement?>>? ExternalReferenceResolver;

        /// <summary>
        /// An instance of the FhirPath compiler to use when evaluating constraints
        /// (provide this if you have custom functions included in the symbol table).
        /// </summary>
        public FhirPathCompiler? FhirPathCompiler;

        /// <summary>
        /// Determines how to deal with failures of FhirPath constraints marked as "best practice". Default is <see cref="ValidateBestPractices.Ignore"/>.
        /// </summary>
        /// <remarks>See <see cref="FhirPathAssertion.BestPractice"/>, <see cref="ValidateBestPractices"/> and
        /// https://www.hl7.org/fhir/best-practices.html for more information.</remarks>
        public ValidateBestPractices ConstraintBestPractices = ValidateBestPractices.Ignore;

        /// <summary>
        /// A function to include the assertion in the validation or not. If the function is left empty (null) then all the 
        /// assertions are processed in the validation.
        /// </summary>
        public Func<IAssertion, bool>? IncludeFilter;

        /// <summary>
        /// A function to exclude the assertion in the validation or not. If the function is left empty (null) then all the 
        /// assertions are processed in the validation.
        /// </summary>
        public Func<IAssertion, bool>? ExcludeFilter;

        /// <summary>
        /// Determines whether a given assertion is included in the validation. The outcome is determined by
        /// <see cref="IncludeFilter"/> and <see cref="ExcludeFilter"/>.
        /// </summary>
        public bool Filter(IAssertion a) =>
                (IncludeFilter is null || IncludeFilter(a)) &&
                (ExcludeFilter is null || !ExcludeFilter(a));

        /// <summary>
        /// This <see cref="ValidationContext"/> can be used when doing trivial validations that do not require terminology services or
        /// reference other schemas. When any of these required dependencies are accessed, a <see cref="NotSupportedException"/> will
        /// be thrown.
        /// </summary>
        public static ValidationContext BuildMinimalContext(IValidateCodeService? validateCodeService = null,
            IElementSchemaResolver? schemaResolver = null, FhirPathCompiler? fpCompiler = null) =>
            new(schemaResolver ?? new NoopSchemaResolver(), validateCodeService ?? new NoopValidateCodeService())
            {
                FhirPathCompiler = fpCompiler
            };

        /// <inheritdoc cref="" />
        public static ValidationContext BuildMinimalContext(ITerminologyServiceNEW terminologyService, IElementSchemaResolver? schemaResolver = null,
            FhirPathCompiler? fpCompiler = null) =>
            new(schemaResolver ?? new NoopSchemaResolver(), terminologyService ?? new NoopTerminologyService())
            {
                FhirPathCompiler = fpCompiler
            };

        internal class NoopSchemaResolver : IElementSchemaResolver
        {
            public Task<IElementSchema?> GetSchema(Uri schemaUri) => throw new NotSupportedException();
        }

        internal class NoopValidateCodeService : IValidateCodeService
        {
            public Task<CodeValidationResult> ValidateCode(string valueSetUrl, Code code, bool abstractAllowed) => throw new NotSupportedException();
            public Task<CodeValidationResult> ValidateConcept(string valueSetUrl, Concept cc, bool abstractAllowed) => throw new NotSupportedException();
        }

        internal class NoopTerminologyService : ITerminologyServiceNEW
        {
            public Task<Assertions> ValidateCode(string? canonical = null, string? context = null, string? code = null, string? system = null, string? version = null, string? display = null, Hl7.Fhir.Model.Coding? coding = null, Hl7.Fhir.Model.CodeableConcept? codeableConcept = null, Hl7.Fhir.Model.FhirDateTime? date = null, bool? @abstract = null, string? displayLanguage = null) =>
                throw new NotSupportedException();
        }
    }
}
