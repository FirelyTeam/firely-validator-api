using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class ConstraintsCorrector(string baseUrl) : Corrector
{
    private class ConstraintExpressionCorrector(string oldExpression, string newExpression)
    {
        public void Correct(ConstraintComponent constraint)
        {
            if (constraint.Expression == oldExpression)
                constraint.Expression = newExpression;
        }
    }

    /// <summary>
    /// The constraint expression correctors are organized by
    /// - FHIR release (e.g. STU3)
    /// - then by path (e.g. "Bundle.entry")
    /// - then by constraint key (e.g. "eld-1")
    /// </summary>
    private readonly Dictionary<FhirRelease, Dictionary<string, Dictionary<string, ConstraintExpressionCorrector>>> _constraintExpressionCorrectors = [];

    /// <summary>
    /// The constraint creators for missing constraints are organized by
    /// - FHIR release (e.g. R4)
    /// - then by path (e.g. "Bundle")
    /// - then by constraint key (e.g. "bdl-3a")
    /// </summary>
    private readonly Dictionary<FhirRelease, Dictionary<string, Dictionary<string, Func<ConstraintComponent>>>> _constraintCreators = [];

    public override void CorrectDifferential(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements, string url) => correct(fhirRelease, elements, url);
    public override void CorrectSnapshot(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements) => correct(fhirRelease, elements, null);

    private void correct(FhirRelease? fhirRelease, ICollection<ElementDefinition> elements, string? url)
    {
        if (!fhirRelease.HasValue)
            return; // Don't really know what we should do if we don't know the FHIR release so it's probably better to do nothing

        correctInvalidConstraints(fhirRelease.Value, elements);
        correctMissingConstraints(fhirRelease.Value, elements, url);
    }

    private void correctInvalidConstraints(FhirRelease fhirRelease, ICollection<ElementDefinition> elements)
    {
        // Get registered constraint correctors for FHIR release
        if (!_constraintExpressionCorrectors.TryGetValue(fhirRelease, out var correctorsForRelease))
            return;

        // Filter and group elements on path
        var pathGroups = elements.Where(e => correctorsForRelease.ContainsKey(e.Path!)).GroupBy(e => e.Path);

        foreach (var pathGroup in pathGroups)
        {
            var correctorsForPath = correctorsForRelease[pathGroup.Key!];

            foreach (var constraint in pathGroup.SelectMany(e => e.Constraint))
            {
                if (correctorsForPath.TryGetValue(constraint.Key!, out var correctorForKey))
                    correctorForKey.Correct(constraint);
            }
        }
    }

    private void correctMissingConstraints(FhirRelease fhirRelease, ICollection<ElementDefinition> elements, string? url)
    {
        // Get registered constraint creators for FHIR release
        if (!_constraintCreators.TryGetValue(fhirRelease, out var creatorsForRelease))
            return;

        // Always correct snapshot (url == null) and only correct differential (url != null) if the URL of the StructureDefinition matches the base URL
        if (url != null && url != baseUrl)
            return;

        // Filter and group elements on path
        var pathGroups = elements.Where(e => creatorsForRelease.ContainsKey(e.Path!)).GroupBy(e => e.Path);

        foreach (var pathGroup in pathGroups)
        {
            var creatorsForPath = creatorsForRelease[pathGroup.Key!];

            foreach (var elemDef in pathGroup)
            {
                var existingKeys = elemDef.Constraint.Select(c => c.Key).ToHashSet();

                foreach (var (key, createKey) in creatorsForPath)
                {
                    if (!existingKeys.Contains(key))
                        elemDef.Constraint.Add(createKey());
                }
            }
        }
    }

    protected void RegisterInvalidConstraint(string path, string key, string oldExpression, string newExpression, params FhirRelease[] releases)
    {
        if (releases.Length == 0)
            return;

        foreach (var release in releases)
        {
            if (!_constraintExpressionCorrectors.TryGetValue(release, out var constraintCorrectorsForRelease))
            {
                constraintCorrectorsForRelease = [];
                _constraintExpressionCorrectors.Add(release, constraintCorrectorsForRelease);
            }

            if (!constraintCorrectorsForRelease.TryGetValue(path, out var constraintCorrectorsForPath))
            {
                constraintCorrectorsForPath = [];
                constraintCorrectorsForRelease.Add(path, constraintCorrectorsForPath);
            }

            constraintCorrectorsForPath[key] = new ConstraintExpressionCorrector(oldExpression, newExpression);
        }
    }

    protected void RegisterMissingConstraint(string path, string key, Func<ConstraintComponent> createConstraint, params FhirRelease[] releases)
    {
        if (releases.Length == 0)
            return;

        foreach (var release in releases)
        {
            if (!_constraintCreators.TryGetValue(release, out var constraintCreatorsForRelease))
            {
                constraintCreatorsForRelease = [];
                _constraintCreators.Add(release, constraintCreatorsForRelease);
            }

            if (!constraintCreatorsForRelease.TryGetValue(path, out var constraintCreatorsForPath))
            {
                constraintCreatorsForPath = [];
                constraintCreatorsForRelease.Add(path, constraintCreatorsForPath);
            }

            constraintCreatorsForPath[key] = createConstraint;
        }
    }
}