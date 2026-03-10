using Hl7.Fhir.Model;
using Hl7.Fhir.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.ElementDefinition;

namespace Firely.Fhir.Validation.Compilation;

internal abstract class ConstraintsCorrector : Corrector
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

    protected override void CorrectElements(FhirRelease? fhirRelease, StructureDefinition sd, ICollection<ElementDefinition> elements)
    {
        // CorrectElements invalid constraints

        if (!fhirRelease.HasValue)
            return; // Don't really know what we should do if we don't know the FHIR release so it's probably better to do nothing
        
        // Get registered constraint correctors for FHIR release
        if (!_constraintExpressionCorrectors.TryGetValue(fhirRelease.Value, out var correctorsForRelease))
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

    protected override void CorrectSnapshotOnlyElements(FhirRelease? fhirRelease, StructureDefinition sd, ICollection<ElementDefinition> elements)
    {
        // Add missing constraints

        if (!fhirRelease.HasValue)
            return; // Don't really know what we should do if we don't know the FHIR release so it's probably better to do nothing

        // Get registered constraint creators for FHIR release
        if (!_constraintCreators.TryGetValue(fhirRelease.Value, out var creatorsForRelease))
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