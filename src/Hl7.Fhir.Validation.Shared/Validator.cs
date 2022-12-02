/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationOutcome = Hl7.Fhir.Model.OperationOutcome;
using StructureDefinition = Hl7.Fhir.Model.StructureDefinition;

namespace Hl7.Fhir.Validation
{
    /// <summary>
    /// This is a shim, simulating the interface of the original SDK validator (pre 5.0 SDK) on top of the
    /// new Firely Validator SDK.
    /// </summary>
    public class Validator
    {
        /// <summary>
        /// The current settings used by the validator when calling one of the Validate methods.
        /// </summary>
        public ValidationSettings Settings { get; private set; }

        /// <summary>
        /// When the validator encounters a StructureDefinition without a snapshot, and <see cref="ValidationSettings.GenerateSnapshot"/> is true,
        /// this event will be invoked to obtain a snapshot for that StructureDefinition.
        /// </summary>
        /// <remarks>If this event is not set, a snapshot will be created using a <see cref="SnapshotGenerator"/> with
        /// the settings in <see cref="ValidationSettings.GenerateSnapshotSettings"/> or the default settings if those
        /// are not set. This snapshot generator will use the <see cref="ValidationSettings.ResourceResolver"/> to fetch
        /// additional StructureDefinitions encountered while snapshotting.</remarks>
        public event EventHandler<OnSnapshotNeededEventArgs>? OnSnapshotNeeded;

        /// <summary>
        /// When <see cref="ValidationSettings.ResolveExternalReferences"/> is true, this event will be called to
        /// resolve references encountered in the instance data (including those encountered in calls to the
        /// FhirPath <c>resolve()</c> function).
        /// </summary>
        /// <remarks>If this event is not set, or resolution resulted in an Exception, resolution is tried using
        /// the <see cref="ValidationSettings.ResourceResolver"/>, by calling <see cref="IResourceResolver.ResolveByUri(string)"/>.</remarks>
        public event EventHandler<OnResolveResourceReferenceEventArgs>? OnExternalResolutionNeeded;

        private SnapshotGenerator? _snapshotGenerator;

        internal SnapshotGenerator? SnapshotGenerator
        {
            get
            {
                if (_snapshotGenerator == null)
                {
                    var resolver = Settings.ResourceResolver;
                    if (resolver != null)
                    {
                        SnapshotGeneratorSettings settings = Settings.GenerateSnapshotSettings ?? SnapshotGeneratorSettings.CreateDefault();
                        _snapshotGenerator = new SnapshotGenerator(resolver, settings);
                    }

                }
                return _snapshotGenerator;
            }
        }

        private FhirPathCompiler? _fpCompiler;

        /// <summary>
        /// The FhirPath compiler to use for handling the FhirPath-based invariants in the
        /// StructureDefinition.
        /// </summary>
        /// <remarks>This property will return <see cref="ValidationSettings.FhirPathCompiler"/> if set,
        /// otherwise it will construct a singleton FhirPathCompiler with all FHIR extensions installed.</remarks>
        internal FhirPathCompiler FpCompiler
        {
            get
            {
                // Use a provided compiler
                if (Settings.FhirPathCompiler != null)
                    return Settings.FhirPathCompiler;

                if (_fpCompiler == null)
                {
                    var symbolTable = new SymbolTable();
                    symbolTable.AddStandardFP();
                    symbolTable.AddFhirExtensions();

                    _fpCompiler = new FhirPathCompiler(symbolTable);

                    Settings.FhirPathCompiler = _fpCompiler;
                }

                return _fpCompiler;
            }
        }

        private readonly YetAnotherInMemoryProvider _yetAnotherInMemoryProvider = new();

        /// <summary>
        /// Construct a new validator with the given settings.
        /// </summary>
        /// <param name="settings"></param>
        public Validator(ValidationSettings settings)
        {
            if (settings.ResourceResolver is null)
                throw new NotSupportedException("The shims provided for backwards compatibility requires that at least a ResourceResolver is set in the settings.");

            Settings = settings.Clone();

            // Generate a schema converter to use to build a sd -> elementschema resolver.
            // We will create a cached one, whose lifetime is bound to this validator.
            // Note also that this means that changing some specific settings (ResourceMapping, ResourceResolver)
            // *after* creating a Validator will no longer influence the conversion of structuredefinitions.
            // I don't expect this to be a problem in practice.
            TypeNameMapper? typenameMapper = settings.ResourceMapping is not null ? mappingWrapper : null;
            var sources = new MultiResolver(_yetAnotherInMemoryProvider, Settings.ResourceResolver!);
            var snapshotSimulator = new SimulateSnapshotHandlingResolver(this, sources);

            var scSettings = new SchemaConverterSettings(snapshotSimulator)
            {
                TypeNameMapper = typenameMapper
            };

            _schemaResolver = StructureDefinitionToElementSchemaResolver.CreatedCached(scSettings);
        }

        private readonly IElementSchemaResolver _schemaResolver;

        /// <summary>
        /// Construct a new validator with default settings.
        /// </summary>
        public Validator() : this(ValidationSettings.CreateDefault())
        {
        }

        /// <summary>
        /// Validate an instance, use the instance's <see cref="ITypedElement.InstanceType"/> to pick the relevant profile to validate against.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance) => Validate(instance, Enumerable.Empty<string>());

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, params string[] canonicals) =>
            Validate(instance, (IEnumerable<string>)canonicals);

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, params StructureDefinition[] structureDefinitions) =>
            Validate(instance, (IEnumerable<StructureDefinition>)structureDefinitions);

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, IEnumerable<StructureDefinition> structureDefinitions)
        {
            _yetAnotherInMemoryProvider.Set(structureDefinitions);
            return Validate(instance, structureDefinitions.Select(sd => sd.Url));
        }

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, IEnumerable<string> canonicals)
        {
            var canonicalsArray = canonicals.Select(c => new Canonical(c)).ToArray();
            var fetchResults = FhirSchemaGroupAnalyzer.FetchSchemas(_schemaResolver, instance.Location, canonicalsArray);

            var failures = fetchResults.Where(fr => !fr.Success).ToList();
            if (failures.Any())
            {
                var message = "Could not fetch the following profiles: " + string.Join(",", failures.Select(f => f.Canonical));
                throw new ArgumentException(message, nameof(canonicals));
            }

            return ValidateInternal(instance, fetchResults.Where(fr => fr.Success).Select(fr => fr.Schema!));
        }

        // This is the one and only main internal entry point for all validations, which in its term
        // will call step 1 in the validator, the function validateElement
        internal OperationOutcome ValidateInternal(ITypedElement instance, IEnumerable<ElementSchema> schemas)
        {
            var vc = convertSettingsToContext();

            try
            {
                var report = schemas.First().Validate(instance, vc);
                return report.RemoveDuplicateEvidence().ToOperationOutcome();
            }
            catch (StructuralTypeException te)
            {
                // Thes should catch all errors caused by navigating the ITypedElement tree.
                var outcome = new OperationOutcome();
                outcome.AddIssue(te.Message, Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, instance.Location);
                return outcome;
            }
            catch (FormatException fe)
            {
                // These should catch all errors caused by navigating the ITypedElement tree.
                // TODO: What kind of issue should this be?  The old validator didn't seem to catch these.
                var outcome = new OperationOutcome();
                outcome.AddIssue(fe.Message, Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, instance.Location);
                return outcome;
            }
        }

        private ValidationContext convertSettingsToContext()
        {
            TypeNameMapper? typenameMapper = Settings.ResourceMapping is not null ? mappingWrapper : null;

            var newContext = new ValidationContext(_schemaResolver, tsFromSettings())
            {
                TypeNameMapper = typenameMapper,

                // The old validator returned a warning, which will also happen when setting this to null.
                HandleValidateCodeServiceFailure = null,

                ResolveExternalReference = Settings.ResolveExternalReferences ? simulateReferenceResolving : null,
                FhirPathCompiler = Settings.FhirPathCompiler ?? FpCompiler,
                ConstraintBestPractices = (ValidateBestPracticesSeverity)Settings.ConstraintBestPracticesSeverity,

                // This will still mean an error is produced on a modifierExtension, just like the behaviour
                // in the old validator.
                FollowExtensionUrl = (_, _) => ValidationContext.ExtensionUrlHandling.WarnIfMissing,

                ExcludeFilter = simulateConstraintSettings,

                TraceEnabled = Settings.Trace
            };

            return newContext;
        }

        private ITypedElement? simulateReferenceResolving(string canonical, string location)
        {
            try
            {
                if (OnExternalResolutionNeeded is not null)
                {
                    var eventArgs = new OnResolveResourceReferenceEventArgs(canonical);
                    OnExternalResolutionNeeded.Invoke(this, eventArgs);
                    return eventArgs.Result;
                }
            }
            catch
            {
                // fall through, as we will try to resolve using the IResourceResolvern now.
            }

            return Settings.ResourceResolver is { } rr ?
               rr.ResolveByUri(canonical.ToString())?.ToTypedElement() : null;
        }

        private bool simulateConstraintSettings(IAssertion a)
        {
            if (a is InvariantValidator inv)
            {
                if (Settings.SkipConstraintValidation) return true;

                if (Settings.ConstraintsToIgnore.Contains(inv.Key)) return true;
            }

            return false;
        }

        private Canonical? mappingWrapper(string tn) => Settings.ResourceMapping!(tn, out var canonical) ? new Canonical(canonical!) : null;

        private ICodeValidationTerminologyService tsFromSettings()
        {
            return Settings switch
            {
                { TerminologyService: not null } => Settings.TerminologyService,
                { ResourceResolver: null } =>
                    throw new NotSupportedException("Cannot resolve binding references since neither TerminologyService nor ResourceResolver is given in the settings"),
                { ResourceResolver: not null } => new LocalTerminologyService(Settings.ResourceResolver.AsAsync()),
            };
        }


        internal OperationOutcome SnapshotGenerationNeeded(StructureDefinition definition)
        {
            if (!Settings.GenerateSnapshot) return new();

            // Default implementation: call event
            if (OnSnapshotNeeded is not null && Settings.ResourceResolver is not null)
            {
                var eventData = new OnSnapshotNeededEventArgs(definition, Settings.ResourceResolver);
                OnSnapshotNeeded(this, eventData);
                return eventData.Result ?? new OperationOutcome();
            }

            // Else, expand, depending on our configuration
            var generator = SnapshotGenerator;
            if (generator != null)
            {
                TaskHelper.Await(() => generator.UpdateAsync(definition));
                return generator.Outcome ?? new OperationOutcome();
            }

            return new OperationOutcome();
        }
    }


    internal class SimulateSnapshotHandlingResolver : IAsyncResourceResolver
    {
        private readonly IResourceResolver _wrapped;

        public SimulateSnapshotHandlingResolver(Validator validator, IResourceResolver wrapped)
        {
            Validator = validator;
            _wrapped = wrapped;
        }

        public Validator Validator { get; }

        public Task<Model.Resource?> ResolveByCanonicalUriAsync(string uri)
        {
            var result = _wrapped.ResolveByCanonicalUri(uri);
            return Task.FromResult(addSnapshot(result));
        }
        public Task<Model.Resource?> ResolveByUriAsync(string uri)
        {
            var result = _wrapped.ResolveByUri(uri);
            return Task.FromResult(addSnapshot(result));
        }

        private Model.Resource? addSnapshot(Model.Resource r)
        {
            if (r is StructureDefinition sd && !sd.HasSnapshot)
            {
                var result = Validator.SnapshotGenerationNeeded(sd);
                return result.Success ? sd : (Model.Resource?)null;
            }
            else
                return r;
        }
    }

    /// <summary>
    /// Arguments supplied to the <see cref="Validator.OnSnapshotNeeded"/> event when invoked.
    /// </summary>
    public class OnSnapshotNeededEventArgs : EventArgs
    {
        /// <summary>
        /// Construct new events args.
        /// </summary>
        public OnSnapshotNeededEventArgs(StructureDefinition definition, IResourceResolver resolver)
        {
            Definition = definition;
            Resolver = resolver;
        }

        /// <summary>
        /// The <see cref="StructureDefinition"/> which needs to be snapshotted. The event should
        /// generate the snapshot in the <see cref="StructureDefinition.Snapshot"/> property of this
        /// instance.
        /// </summary>
        public StructureDefinition Definition { get; }

        /// <summary>
        /// The resolver to use when generating the snapshot.
        /// </summary>
        public IResourceResolver Resolver { get; }

        /// <summary>
        /// An <see cref="OperationOutcome"/> that represents success or issues encountered while
        /// creating the snapshot.
        /// </summary>
        public OperationOutcome? Result { get; set; }
    }

    /// <summary>
    /// Arguments supplied to the <see cref="Validator.OnExternalResolutionNeeded"/> event when invoked.
    /// </summary>
    public class OnResolveResourceReferenceEventArgs : EventArgs
    {
        /// <summary>
        /// Construct new events args.
        /// </summary>
        public OnResolveResourceReferenceEventArgs(string reference)
        {
            Reference = reference;
        }

        /// <summary>
        /// The reference that should be resolved to an <see cref="ITypedElement"/> instance.
        /// </summary>
        public string Reference { get; }

        /// <summary>
        /// The resolved instance.
        /// </summary>
        public ITypedElement? Result { get; set; }
    }


    internal class YetAnotherInMemoryProvider : IAsyncResourceResolver
    {
        private readonly List<StructureDefinition> _inMemorySds = new();

        public Task<Model.Resource> ResolveByCanonicalUriAsync(string uri) =>
            Task.FromResult((Model.Resource)_inMemorySds.Where(sd => sd.Url == uri).FirstOrDefault());
        public Task<Model.Resource> ResolveByUriAsync(string uri) => ResolveByCanonicalUriAsync(uri);

        public void Set(IEnumerable<StructureDefinition> inMemorySds)
        {
            _inMemorySds.Clear();
            _inMemorySds.AddRange(inMemorySds);
        }
    }

    // Marked obsolete at 2022-11-22, EK
    [Obsolete("This enumeration is not used (publicly) by the SDK and will be removed from the public surface in the future.")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum BatchValidationMode
    {
        All,
        Any,
        Once
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
