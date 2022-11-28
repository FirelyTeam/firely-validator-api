/* 
 * Copyright (c) 2016, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Navigation;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Support;
using Hl7.FhirPath;
using Hl7.FhirPath.Expressions;
using System;
using System.Collections.Generic;
using System.IO;

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

        /// <summary>
        /// Construct a new validator with the given settings.
        /// </summary>
        /// <param name="settings"></param>
        public Validator(ValidationSettings settings)
        {
            Settings = settings.Clone();
        }

        /// <summary>
        /// Construct a new validator with default settings.
        /// </summary>
        public Validator() : this(ValidationSettings.CreateDefault())
        {
        }

        /// <summary>
        /// Validate an instance, use the instance's <see cref="ITypedElement.InstanceType"/> to pick the relevant profile to validate against.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance)
        {
            throw new NotImplementedException();
            //var state = new ValidationState();
            //var result = ValidateInternal(instance, declaredTypeProfile: null, statedCanonicals: null, statedProfiles: null, state: state)
            //    .RemoveDuplicateMessages();
            //result.SetAnnotation(state);
            //return result;
        }

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, params string[] definitionUris) =>
            Validate(instance, (IEnumerable<string>)definitionUris);

        /// <summary>
        /// Validate an instance against a given set of profiles.
        /// </summary>
        public OperationOutcome Validate(ITypedElement instance, IEnumerable<string> definitionUris)
        {
            throw new NotImplementedException();
            //var state = new ValidationState();
            //var result = ValidateInternal(instance, declaredTypeProfile: null, statedCanonicals: definitionUris, statedProfiles: null, state: state)
            //    .RemoveDuplicateMessages();
            //result.SetAnnotation(state);
            //return result;
        }

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
            throw new NotImplementedException();
            //var state = new ValidationState();
            //var result = ValidateInternal(
            //    instance,
            //    declaredTypeProfile: null,
            //    statedCanonicals: null,
            //    statedProfiles: structureDefinitions,
            //    state: state).RemoveDuplicateMessages();
            //result.SetAnnotation(state);
            //return result;
        }

        // This is the one and only main entry point for all external validation calls (i.e. invoked by the user of the API)
        internal OperationOutcome ValidateInternal(
            ITypedElement instance,
            string declaredTypeProfile,
            IEnumerable<string> statedCanonicals,
            IEnumerable<StructureDefinition> statedProfiles,
            object state) // used to be ValidationState
        {
            throw new NotImplementedException();
            //var resolutionContext = instance switch
            //{
            //    { InstanceType: "Extension" } when instance.Name == "modifierExtension" => ProfileAssertion.ResolutionContext.InModifierExtension,
            //    { InstanceType: "Extension" } => ProfileAssertion.ResolutionContext.InExtension,
            //    _ => ProfileAssertion.ResolutionContext.Elsewhere
            //};

            //var processor = new ProfilePreprocessor(profileResolutionNeeded, snapshotGenerationNeeded, instance, declaredTypeProfile, statedProfiles, statedCanonicals, Settings.ResourceMapping, resolutionContext);
            //var outcome = processor.Process();

            //// Note: only start validating if the profiles are complete and consistent
            //if (outcome.Success)
            //    outcome.Add(ValidateInternal(instance, processor.Result, state));

            //return outcome;
        }

        private StructureDefinition? profileResolutionNeeded(string canonical) =>
                //TODO: Need to make everything async in 2.x validator
#pragma warning disable CS0618 // Type or member is obsolete
                Settings.ResourceResolver?.FindStructureDefinition(canonical);
#pragma warning restore CS0618 // Type or member is obsolete


        internal OperationOutcome ValidateInternal(ITypedElement instance, ElementDefinitionNavigator definition, object state)
            => ValidateInternal(instance, new[] { definition }, state).RemoveDuplicateMessages();


        // This is the one and only main internal entry point for all validations, which in its term
        // will call step 1 in the validator, the function validateElement
        internal OperationOutcome ValidateInternal(ITypedElement elementNav, IEnumerable<ElementDefinitionNavigator> definitions, object state)
        {
            throw new NotImplementedException();
            //var outcome = new OperationOutcome();
            //var instance = elementNav.ToScopedNode();

            //try
            //{
            //    var allDefinitions = definitions.ToList();

            //    if (allDefinitions.Count() == 1)
            //        outcome.Add(startValidation(allDefinitions.Single(), instance, state));
            //    else
            //    {
            //        var validators = allDefinitions.Select(nav => createValidator(nav));
            //        outcome.Add(this.Combine("the list of profiles", BatchValidationMode.All, instance, validators));
            //    }
            //}
            //catch (Exception e)
            //{
            //    outcome.AddIssue($"Internal logic failure: {e.Message}", Issue.PROCESSING_CATASTROPHIC_FAILURE, instance);
            //}

            //return outcome;

            //Func<OperationOutcome> createValidator(ElementDefinitionNavigator nav) =>
            //    () => startValidation(nav, instance, state);

        }

        private OperationOutcome startValidation(ElementDefinitionNavigator definition, ScopedNode instance, object state)
        {
            throw new NotImplementedException();
            //// If we are starting a validation of a referenceable element (resource, contained resource, nested resource),
            //// make sure we keep track of it, so we can detect loops and avoid validating the same resource multiple times.
            //if (instance.AtResource && definition.AtRoot)
            //{
            //    state.Global.ResourcesValidated.Increase();
            //    var location = state.Instance.ExternalUrl is string extu
            //        ? extu + "#" + instance.Location
            //        : instance.Location;

            //    return state.Instance.InternalValidations.Start(location, definition.StructureDefinition.Url,
            //        () => validateElement(definition, instance, state));
            //}
            //else
            //{
            //    return validateElement(definition, instance, state);
            //}
        }

        private OperationOutcome validateElement(ElementDefinitionNavigator definition, ScopedNode instance, object state)
        {
            var outcome = new OperationOutcome();

            try
            {
                throw new NotImplementedException();
            }
            catch (StructuralTypeException te)
            {
                outcome.AddIssue(te.Message, Issue.CONTENT_ELEMENT_HAS_INCORRECT_TYPE, instance.Location);
                return outcome;
            }
        }


        internal OperationOutcome.IssueComponent? Trace(OperationOutcome outcome, string message, Issue issue, string location) =>
            Settings.Trace || issue.Severity != OperationOutcome.IssueSeverity.Information
                ? outcome.AddIssue(message, issue, location)
                : null;

        internal OperationOutcome.IssueComponent? Trace(OperationOutcome outcome, string message, Issue issue, ITypedElement location) =>
            Settings.Trace || issue.Severity != OperationOutcome.IssueSeverity.Information
                ? Trace(outcome, message, issue, location.Location)
                : null;

        internal ITypedElement? ExternalReferenceResolutionNeeded(string reference, OperationOutcome outcome, string path)
        {
            if (!Settings.ResolveExternalReferences) return null;

            try
            {
                // Default implementation: call event
                if (OnExternalResolutionNeeded != null)
                {
                    var args = new OnResolveResourceReferenceEventArgs(reference);
                    OnExternalResolutionNeeded(this, args);
                    return args.Result;
                }
            }
            catch (Exception e)
            {
                Trace(outcome, "External resolution of '{reference}' caused an error: " + e.Message, Issue.UNAVAILABLE_REFERENCED_RESOURCE, path);
            }

            // Else, try to resolve using the given ResourceResolver 
            // (note: this also happens when the external resolution above threw an exception)
            if (Settings.ResourceResolver != null)
            {
                try
                {
                    var poco = Settings.ResourceResolver.ResolveByUri(reference);
                    if (poco != null)
                        return poco.ToTypedElement();
                }
                catch (Exception e)
                {
                    Trace(outcome, $"Resolution of reference '{reference}' using the Resolver SDK failed: " + e.Message, Issue.UNAVAILABLE_REFERENCED_RESOURCE, path);
                }
            }

            return null;        // Sorry, nothing worked
        }


        // Note: this modifies an SD that is passed to us and will alter a possibly cached
        // object shared amongst other threads. This is generally useful and saves considerable
        // time when the same snapshot is needed again, but may result in side-effects
        private OperationOutcome snapshotGenerationNeeded(StructureDefinition definition)
        {
            if (!Settings.GenerateSnapshot) return new OperationOutcome();

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
                //TODO: make everything async in 2.x validator
#pragma warning disable CS0618 // Type or member is obsolete
                generator.Update(definition);
#pragma warning restore CS0618 // Type or member is obsolete

#if DEBUG
                // TODO: Validation Async Support
                string xml = (new FhirXmlSerializer()).SerializeToString(definition);
                string name = definition.Id ?? definition.Name.Replace(" ", "").Replace("/", "");
                var dir = Path.Combine(Path.GetTempPath(), "validation");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(Path.Combine(dir, name) + ".StructureDefinition.xml", xml);
#endif


                return generator.Outcome ?? new OperationOutcome();
            }

            return new OperationOutcome();
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
