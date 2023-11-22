/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Utility;
using System;
using System.Xml.Schema;

namespace Hl7.Fhir.Validation
{
    /// <summary>
    /// Configuration settings for the <see cref="Validator"/> class.
    /// </summary>
    public class ValidationSettings
    {
        /// <summary>
        /// The <see cref="StructureDefinitionSummaryProvider.TypeNameMapper"/> used to map instance types encountered in
        /// <see cref="IBaseElementNavigator{IScopedNode}.InstanceType"/> to canonicals when a profile for these types needs to be retrieved.
        /// </summary>
        public StructureDefinitionSummaryProvider.TypeNameMapper? ResourceMapping { get; set; }

        /// <summary>
        /// The resolver to use when canonicals or references to other resources are encountered in the instance.
        /// </summary>
        /// <remarks>Most of the time this resolver is used when resolving canonicals to profiles or valuesets, 
        /// but it may also be used to resolve references encountered in instance data. 
        /// See <see cref="Validator.OnExternalResolutionNeeded"/> for more information.</remarks>
        public IResourceResolver? ResourceResolver { get; set; }

        /// <summary>
        /// The terminology service to use to validate coded instance data.
        /// </summary>
        public ITerminologyService? TerminologyService { get; set; }

        /// <summary>
        /// An instance of the FhirPath compiler to use when evaluating constraints
        /// (provide this if you have custom functions included in the symbol table)
        /// </summary>
        /// <remarks>If this property is not set, the validator will 
        /// use a FhirPathCompiler with all FHIR extensions installed.</remarks>
        public Hl7.FhirPath.FhirPathCompiler? FhirPathCompiler { get; set; }

        /// <summary>
        /// The validator needs StructureDefinitions to have a snapshot form to function. If a StructureDefinition
        /// without a snapshot is encountered, should the validator generate the snapshot from the differential
        /// present in the StructureDefinition? Default is 'false'.
        /// </summary>
        public bool GenerateSnapshot { get; set; } // = false

        /// <summary>
        /// Configuration settings for the snapshot generator
        /// (if the <see cref="GenerateSnapshot"/> property is enabled).
        /// <para>Never returns <c>null</c>. Assigning <c>null</c> reverts back to default settings.</para>
        /// </summary>
        public SnapshotGeneratorSettings? GenerateSnapshotSettings
        {
            get => _generateSnapshotSettings;
            set => _generateSnapshotSettings = value?.Clone() ?? SnapshotGeneratorSettings.CreateDefault();
        }

        private SnapshotGeneratorSettings _generateSnapshotSettings = SnapshotGeneratorSettings.CreateDefault();

        /// <summary>
        /// Include informational tracing information in the validation output. Useful for debugging purposes. Default is 'false'.
        /// </summary>
        public bool Trace { get; set; } // = false;

        /// <summary>
        /// StructureDefinition may contain FhirPath constraints to enfore invariants in the data that cannot
        /// be expresses using StructureDefinition alone. This validation can be turned off for performance or
        /// debugging purposes. Default is 'false'.
        /// </summary>
        public bool SkipConstraintValidation { get; set; } // = false;

        /// <summary>
        /// A list of constraints to be ignored by the validator. Default values are dom-6, rng-2, "bdl-8" and "cnl-0"
        /// </summary>
        public string[]? ConstraintsToIgnore { get; set; } = new string[] { "dom-6", "rng-2", "bdl-8", "cnl-0" };

        /// <summary>
        /// If a reference is encountered that references to a resource outside of the current instance being validated,
        /// this setting controls whether the validator will call out to the ResourceResolver to try to resolve the
        /// external reference.
        /// </summary>
        /// <remarks>References that refer to resources inside the current instance (i.e.
        /// contained resources, Bundle entries) will always be followed and validated. See <see cref="Validator.OnExternalResolutionNeeded"/> 
        /// for more information.</remarks>
        public bool ResolveExternalReferences { get; set; } // = false;

        /// <summary>
        /// If set to true (and the XDocument specific overloads of validate() are used), the validator will run
        /// .NET XSD validation prior to running profile validation
        /// </summary>
        public bool EnableXsdValidation { get; set; } // = false;

        /// <summary>
        /// Choose whether the validator will treat the violations of the invariants marked as best practices as errors or as warnings.
        /// </summary>
        public ConstraintBestPracticesSeverity ConstraintBestPracticesSeverity { get; set; }

        /// <summary>
        /// Determine where to retrieve the XSD schemas from when when Xsd validation is enabled and run.
        /// </summary>
        /// <remarks>If this is not set, the default FHIR XSDs from the specification will be used.</remarks>
        public XmlSchemaSet? XsdSchemaCollection { get; set; }

        /// <summary>
        /// Default constructor. Creates a new <see cref="ValidationSettings"/> instance with default property values.
        /// </summary>
        public ValidationSettings() { }

        /// <summary>Clone constructor. Generates a new <see cref="ValidationSettings"/> instance initialized from the state of the specified instance.</summary>
        /// <exception cref="ArgumentNullException">The specified argument is <c>null</c>.</exception>
        public ValidationSettings(ValidationSettings other)
        {
            if (other == null) throw Error.ArgumentNull(nameof(other));
            other.CopyTo(this);
        }

        /// <summary>Copy all configuration settings to another instance.</summary>
        /// <param name="other">Another <see cref="ValidationSettings"/> instance.</param>
        /// <exception cref="ArgumentNullException">The specified argument is <c>null</c>.</exception>
        public void CopyTo(ValidationSettings other)
        {
            if (other == null) throw Error.ArgumentNull(nameof(other));

            other.ConstraintBestPracticesSeverity = ConstraintBestPracticesSeverity;
            other.GenerateSnapshot = GenerateSnapshot;
            other.GenerateSnapshotSettings = GenerateSnapshotSettings?.Clone();
            other.EnableXsdValidation = EnableXsdValidation;
            other.ResolveExternalReferences = ResolveExternalReferences;
            other.ResourceResolver = ResourceResolver;
            other.SkipConstraintValidation = SkipConstraintValidation;
            other.TerminologyService = TerminologyService;
            other.Trace = Trace;
            other.FhirPathCompiler = FhirPathCompiler;
            other.ResourceMapping = ResourceMapping;
            other.XsdSchemaCollection = XsdSchemaCollection;
            other.ConstraintsToIgnore = ConstraintsToIgnore;
        }

        /// <summary>Creates a new <see cref="ValidationSettings"/> object that is a copy of the current instance.</summary>
        public ValidationSettings Clone() => new(this);

        /// <summary>Creates a new <see cref="ValidationSettings"/> instance with default property values.</summary>
        public static ValidationSettings CreateDefault() => new();

    }
}