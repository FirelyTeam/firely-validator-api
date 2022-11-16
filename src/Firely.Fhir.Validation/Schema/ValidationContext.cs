/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Terminology;
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
        /// <summary>
        /// How to handle the extension Url
        /// </summary>
        public enum ExtensionUrlHandling
        {
            /// <summary>
            /// Do not resolve the extension
            /// </summary>
            DontResolve,
            /// <summary>
            /// Add a warning to the validation result when the extension cannot be resolved
            /// </summary>
            WarnIfMissing,
            /// <summary>
            /// Add an error to the validation result when the extension cannot be resolved
            /// </summary>
            ErrorIfMissing,
        }

        /// <summary>
        /// The validation result when there is an exception in the Terminology Service
        /// </summary>
        public enum TerminologyServiceExceptionResult
        {
            /// <summary>
            /// Return a warning in case of an exception in Terminology Service.
            /// </summary>
            Warning,
            /// <summary>
            /// Return an error in case of an exception in Terminology Service.
            /// </summary>
            Error,
        }

        /// <summary>
        /// Initializes a new ValidationContext with the minimal dependencies.
        /// </summary>
        public ValidationContext(IElementSchemaResolver schemaResolver, ITerminologyService validateCodeService)
        {
            ElementSchemaResolver = schemaResolver ?? throw new ArgumentNullException(nameof(schemaResolver));
            ValidateCodeService = validateCodeService ?? throw new ArgumentNullException(nameof(validateCodeService));
        }

        /// <summary>
        /// An <see cref="ITerminologyService"/> that is used when the validator must validate a code against a 
        /// terminology service.
        /// </summary>
        public ICodeValidationTerminologyService ValidateCodeService;

        /// <summary>
        /// The <see cref="ValidateCodeServiceFailureHandler"/> to invoke when the validator calls out to a terminology service and this call
        /// results in an exception. When no function is set, the validator defaults to returning a warning.
        /// </summary>
        public ValidateCodeServiceFailureHandler? OnValidateCodeServiceFailure = null;

        ///  The function has 2 input parameters: <list>
        /// <item>- valueSetUrl (of type Canonical): the valueSetUrl of the Binding</item>
        /// <item>- codes (of type string): a comma separated list of codings </item>
        /// <item>- abstract: whether a concept designated as 'abstract' is appropriate/allowed to be use or not</item>
        /// <item>- context: the context of the value set</item>
        /// </list>
        /// Result of the function is <see cref="TerminologyServiceExceptionResult"/>.
        /// <summary>
        /// A delegate that determines the result of a failed terminology service call by the validator.
        /// </summary>
        /// <param name="p">The <see cref="Parameters"/> object that was passed to the <seealso href="http://hl7.org/fhir/valueset-operation-validate-code.html">terminology service</seealso>.</param>
        /// <param name="e">The <see cref="FhirOperationException"/> as returned by the service.</param>
        public delegate TerminologyServiceExceptionResult ValidateCodeServiceFailureHandler(ValidateCodeParameters p, FhirOperationException e);

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
        public Func<string, Task<ITypedElement?>>? ExternalReferenceResolver = null;

        /// <summary>
        /// An instance of the FhirPath compiler to use when evaluating constraints
        /// (provide this if you have custom functions included in the symbol table).
        /// </summary>
        public FhirPathCompiler? FhirPathCompiler = null;

        /// <summary>
        /// Determines how to deal with failures of FhirPath constraints marked as "best practice". Default is <see cref="ValidateBestPracticesSeverity.Warning"/>.
        /// </summary>
        /// <remarks>See <see cref="FhirPathValidator.BestPractice"/>, <see cref="ValidateBestPracticesSeverity"/> and
        /// https://www.hl7.org/fhir/best-practices.html for more information.</remarks>
        public ValidateBestPracticesSeverity ConstraintBestPractices = ValidateBestPracticesSeverity.Warning;

        /// <summary>
        /// A function that determines which profiles in meta.profile the validator should use to validate this instance.
        /// The function has 2 input parameters: <list>
        /// <item>- location (of type string): the location of this resource</item>
        /// <item>- originalProfiles (of type Canonical[]): the original list of profiles found in Meta.profile </item>
        /// </list>
        /// Result of the function is a new set of meta profiles that the validator will use for validation of this instance.
        /// </summary>
        public Func<string, Canonical[], Canonical[]>? FollowMetaProfile = null;

        /// <summary>
        /// A function to determine what to do with an extension
        /// The function has 2 input parameters: <list>
        /// <item>- location (of type string): the location of the extension</item>
        /// <item>- extensionUrl (of type Canonical?): the extension Url from the instance</item>
        /// </list>
        /// When no function is set (the property <see cref="FollowExtensionUrl"/> is null), then a validation 
        /// of an Extension will warn if the extension is not present, or return an error when the extension is a modififier extension.
        /// Result of the function is ExtensionUrlHandling.
        /// </summary>
        public Func<string, Canonical?, ExtensionUrlHandling>? FollowExtensionUrl = null;

        /// <summary>
        /// A function to include the assertion in the validation or not. If the function is left empty (null) then all the 
        /// assertions are processed in the validation.
        /// </summary>
        public Func<IAssertion, bool>? IncludeFilter = null;

        /// <summary>
        /// A function to exclude the assertion in the validation or not. If the function is left empty (null) then all the 
        /// assertions are processed in the validation.
        /// </summary>
        public Func<IAssertion, bool>? ExcludeFilter = a => (a is FhirPathValidator fhirPathAssertion && fhirPathAssertion.Key == "dom-6");

        /// <summary>
        /// Determines whether a given assertion is included in the validation. The outcome is determined by
        /// <see cref="IncludeFilter"/> and <see cref="ExcludeFilter"/>.
        /// </summary>
        public bool Filter(IAssertion a) =>
                (IncludeFilter is null || IncludeFilter(a)) &&
                (ExcludeFilter is null || !ExcludeFilter(a));

        /// <summary>
        /// Whether to add trace messages to the validation result.
        /// </summary>
        public bool TraceEnabled = false;

        /// <summary>
        /// Invokes a factory method for assertions only when tracing is on.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public ResultReport TraceResult(Func<TraceAssertion> p) =>
            TraceEnabled ? p().AsResult() : ResultReport.SUCCESS;

        /// <summary>
        /// This <see cref="ValidationContext"/> can be used when doing trivial validations that do not require terminology services or
        /// reference other schemas. When any of these required dependencies are accessed, a <see cref="NotSupportedException"/> will
        /// be thrown.
        /// </summary>
        public static ValidationContext BuildMinimalContext(ITerminologyService? terminologyService = null,
            IElementSchemaResolver? schemaResolver = null, FhirPathCompiler? fpCompiler = null) =>
            new(schemaResolver ?? new NoopSchemaResolver(), terminologyService ?? new NoopTerminologyService())
            {
                FhirPathCompiler = fpCompiler
            };

        /// <summary>
        /// A resolver that just throws <see cref="NotSupportedException"/>. Used to create a minimally
        /// valid ValidationContext that can be used in unit-test that do not use other schemas.
        /// </summary>
        internal class NoopSchemaResolver : IElementSchemaResolver
        {
            public ElementSchema? GetSchema(Canonical schemaUri) => throw new NotSupportedException();
        }

        /// <summary>
        /// A ValidateCodeService that just throws <see cref="NotSupportedException"/>. Used to create a minimally
        /// valid ValidationContext that can be used in unit-test that do not require terminology services.
        /// </summary>
        internal class NoopTerminologyService : ITerminologyService
        {
            public Task<Hl7.Fhir.Model.Resource> Closure(Hl7.Fhir.Model.Parameters parameters, bool useGet = false) => throw new NotImplementedException();
            public Task<Hl7.Fhir.Model.Parameters> CodeSystemValidateCode(Hl7.Fhir.Model.Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Hl7.Fhir.Model.Resource> Expand(Hl7.Fhir.Model.Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Hl7.Fhir.Model.Parameters> Lookup(Hl7.Fhir.Model.Parameters parameters, bool useGet = false) => throw new NotImplementedException();
            public Task<Hl7.Fhir.Model.Parameters> Subsumes(Hl7.Fhir.Model.Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Hl7.Fhir.Model.Parameters> Translate(Hl7.Fhir.Model.Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
            public Task<Hl7.Fhir.Model.Parameters> ValueSetValidateCode(Hl7.Fhir.Model.Parameters parameters, string? id = null, bool useGet = false) => throw new NotImplementedException();
        }
    }
}
