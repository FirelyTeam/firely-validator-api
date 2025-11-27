/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Firely.Fhir.Packages;
using FluentAssertions;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using System.Linq;
using Xunit;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// Tests for validating resources with contained resources.
    /// Specifically tests the fix for the issue where the first resource in contained section 
    /// is shown in the error message although the resource is valid.
    /// </summary>
    public class ContainedResourceValidationTests
    {
        [Fact]
        [Trait("Category", "Validation")]
        public void CarePlanWithTwoContainedGoals_OnlyInvalidGoalShouldAppearInErrorMessage()
        {
            // This test validates that when a CarePlan has two contained Goal resources,
            // and only the second Goal is invalid, the validation error should only reference
            // the invalid (second) Goal, not the valid (first) Goal.
            
            // Arrange - Create two Goal profiles with different fixed URI values
            var goalProfile1 = new StructureDefinition
            {
                Url = "https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g1",
                Name = "Goal_with_fixedurl_g1",
                Status = PublicationStatus.Draft,
                FhirVersion = FHIRVersion.N4_3_0,
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                Type = "Goal",
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Goal",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        new ElementDefinition("Goal.description.coding.system")
                        {
                            ElementId = "Goal.description.coding.system",
                            Path = "Goal.description.coding.system",
                            Fixed = new FhirUri("http://fixed-uri.com/system")
                        }
                    ]
                }
            };

            var goalProfile2 = new StructureDefinition
            {
                Url = "https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g2",
                Name = "Goal_with_fixedurl_g2",
                Status = PublicationStatus.Draft,
                FhirVersion = FHIRVersion.N4_3_0,
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                Type = "Goal",
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Goal",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        new ElementDefinition("Goal.description.coding.system")
                        {
                            ElementId = "Goal.description.coding.system",
                            Path = "Goal.description.coding.system",
                            Fixed = new FhirUri("http://fixed-uri.com/system")
                        }
                    ]
                }
            };

            // Create a CarePlan with two contained Goal resources
            // First Goal is valid, second Goal has incorrect system value
            var carePlan = new CarePlan
            {
                Contained =
                [
                    new Goal
                    {
                        Id = "f873f0480edf4e7da98c01be4c05ad91",
                        Meta = new Meta
                        {
                            Profile = ["https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g1"]
                        },
                        LifecycleStatus = Goal.GoalLifecycleStatus.Active,
                        Description = new CodeableConcept
                        {
                            Coding =
                            [
                                new Coding
                                {
                                    System = "http://fixed-uri.com/system",  // Correct value
                                    Code = "G1"
                                }
                            ]
                        },
                        Subject = new ResourceReference
                        {
                            Reference = "Patient/example",
                            Display = "Peter James Chalmers"
                        }
                    },
                    new Goal
                    {
                        Id = "f873f0480edf4e7da98c01be4c05ad92",
                        Meta = new Meta
                        {
                            Profile = ["https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g2"]
                        },
                        LifecycleStatus = Goal.GoalLifecycleStatus.Active,
                        Description = new CodeableConcept
                        {
                            Coding =
                            [
                                new Coding
                                {
                                    System = "http://incorrect-uri.com/system",  // Incorrect value
                                    Code = "G2"
                                }
                            ]
                        },
                        Subject = new ResourceReference
                        {
                            Reference = "Patient/example",
                            Display = "Peter James Chalmers"
                        }
                    }
                ],
                Status = RequestStatus.Active,
                Intent = CarePlan.CarePlanIntent.Plan,
                Subject = new ResourceReference
                {
                    Reference = "Patient/example",
                    Display = "Peter James Chalmers"
                },
                Goal =
                [
                    new ResourceReference { Reference = "#f873f0480edf4e7da98c01be4c05ad91" },
                    new ResourceReference { Reference = "#f873f0480edf4e7da98c01be4c05ad92" }
                ]
            };

            // Setup validator
            var packageServer = "https://packages.simplifier.net";
            string[] packageNames =
                [
                    "hl7.fhir.r4.core@4.0.1"
                ];
            var packageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packageServer, packageNames);
            var combinedResolver = new MultiResolver(new InMemoryProfileResolver(goalProfile1, goalProfile2), packageResolver);
            
            // Wrap in SnapshotSource to generate snapshots for differential-only profiles
            var profileSource = new SnapshotSource(combinedResolver);

            var terminologyService = new LocalTerminologyService(profileSource.AsAsync());
            var validator = new Validator(profileSource.AsAsync(), terminologyService);

            // Act
            var outcome = validator.Validate(carePlan);

            // Assert
            // There should be validation errors for the invalid Goal
            outcome.Issue.Should().NotBeEmpty("because the second Goal has an incorrect system value");

            // Count how many issues reference the first Goal (the valid one)
            var issuesReferencingFirstGoal = outcome.Issue.Where(issue =>
            {
                var hasLocationMatch = issue.Location != null && issue.Location.Any(loc => loc != null && loc.Contains("contained[0]"));
                var hasExpressionMatch = issue.Expression != null && issue.Expression.Any(exp => exp != null && exp.Contains("contained[0]"));
                return hasLocationMatch || hasExpressionMatch;
            }).ToList();

            // Count how many issues reference the second Goal (the invalid one)
            var issuesReferencingSecondGoal = outcome.Issue.Where(issue =>
            {
                var hasLocationMatch = issue.Location != null && issue.Location.Any(loc => loc != null && loc.Contains("contained[1]"));
                var hasExpressionMatch = issue.Expression != null && issue.Expression.Any(exp => exp != null && exp.Contains("contained[1]"));
                return hasLocationMatch || hasExpressionMatch;
            }).ToList();

            // The key assertion: Only the second (invalid) Goal should be mentioned in error messages
            // The first (valid) Goal should NOT appear in any error messages
            issuesReferencingFirstGoal.Should().BeEmpty(
                "because the first contained Goal is valid and should not generate errors");

            issuesReferencingSecondGoal.Should().NotBeEmpty(
                "because the second contained Goal is invalid and should generate errors");

            // Additionally, verify that the error message mentions the correct profile and system mismatch
            var fixedValueIssues = outcome.Issue.Where(issue =>
            {
                return issue.Details != null && issue.Details.Text != null && issue.Details.Text.Contains("fixed value");
            }).ToList();

            fixedValueIssues.Should().NotBeEmpty(
                "because there should be an error about the fixed value mismatch");

            // Ensure the error mentions the incorrect value
            var hasIncorrectValueMention = fixedValueIssues.Any(issue =>
                issue.Details != null && issue.Details.Text != null && issue.Details.Text.Contains("http://incorrect-uri.com/system"));
            hasIncorrectValueMention.Should().BeTrue(
                "because the error should mention the incorrect system value");

            // Ensure the error mentions the expected fixed value
            var hasFixedValueMention = fixedValueIssues.Any(issue =>
                issue.Details != null && issue.Details.Text != null && issue.Details.Text.Contains("http://fixed-uri.com/system"));
            hasFixedValueMention.Should().BeTrue(
                "because the error should mention the expected fixed value");
        }

        [Fact]
        [Trait("Category", "Validation")]
        public void CarePlanWithTwoContainedGoals_BothValidShouldPassValidation()
        {
            // This test validates that when both contained Goals are valid, no errors are generated.
            
            // Arrange - Create two Goal profiles with the same fixed URI value
            var goalProfile1 = new StructureDefinition
            {
                Url = "https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g1",
                Name = "Goal_with_fixedurl_g1",
                Status = PublicationStatus.Draft,
                FhirVersion = FHIRVersion.N4_3_0,
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                Type = "Goal",
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Goal",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        new ElementDefinition("Goal.description.coding.system")
                        {
                            ElementId = "Goal.description.coding.system",
                            Path = "Goal.description.coding.system",
                            Fixed = new FhirUri("http://fixed-uri.com/system")
                        }
                    ]
                }
            };

            var goalProfile2 = new StructureDefinition
            {
                Url = "https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g2",
                Name = "Goal_with_fixedurl_g2",
                Status = PublicationStatus.Draft,
                FhirVersion = FHIRVersion.N4_3_0,
                Kind = StructureDefinition.StructureDefinitionKind.Resource,
                Abstract = false,
                Type = "Goal",
                BaseDefinition = "http://hl7.org/fhir/StructureDefinition/Goal",
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Differential = new StructureDefinition.DifferentialComponent
                {
                    Element =
                    [
                        new ElementDefinition("Goal.description.coding.system")
                        {
                            ElementId = "Goal.description.coding.system",
                            Path = "Goal.description.coding.system",
                            Fixed = new FhirUri("http://fixed-uri.com/system")
                        }
                    ]
                }
            };

            // Create a CarePlan with two contained Goal resources, both valid
            var carePlan = new CarePlan
            {
                Contained =
                [
                    new Goal
                    {
                        Id = "f873f0480edf4e7da98c01be4c05ad91",
                        Meta = new Meta
                        {
                            Profile = ["https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g1"]
                        },
                        LifecycleStatus = Goal.GoalLifecycleStatus.Active,
                        Description = new CodeableConcept
                        {
                            Coding =
                            [
                                new Coding
                                {
                                    System = "http://fixed-uri.com/system",  // Correct value
                                    Code = "G1"
                                }
                            ]
                        },
                        Subject = new ResourceReference
                        {
                            Reference = "Patient/example",
                            Display = "Peter James Chalmers"
                        }
                    },
                    new Goal
                    {
                        Id = "f873f0480edf4e7da98c01be4c05ad92",
                        Meta = new Meta
                        {
                            Profile = ["https://example.org/fhir/StructureDefinition/Goal-with-fixedurl-g2"]
                        },
                        LifecycleStatus = Goal.GoalLifecycleStatus.Active,
                        Description = new CodeableConcept
                        {
                            Coding =
                            [
                                new Coding
                                {
                                    System = "http://fixed-uri.com/system",  // Correct value
                                    Code = "G2"
                                }
                            ]
                        },
                        Subject = new ResourceReference
                        {
                            Reference = "Patient/example",
                            Display = "Peter James Chalmers"
                        }
                    }
                ],
                Status = RequestStatus.Active,
                Intent = CarePlan.CarePlanIntent.Plan,
                Subject = new ResourceReference
                {
                    Reference = "Patient/example",
                    Display = "Peter James Chalmers"
                },
                Goal =
                [
                    new ResourceReference { Reference = "#f873f0480edf4e7da98c01be4c05ad91" },
                    new ResourceReference { Reference = "#f873f0480edf4e7da98c01be4c05ad92" }
                ]
            };

            // Setup validator
            var packageServer = "https://packages.simplifier.net";
            string[] packageNames =
                [
                    "hl7.fhir.r4.core@4.0.1"
                ];
            var packageResolver = new FhirPackageSource(ModelInfo.ModelInspector, packageServer, packageNames);
            var combinedResolver = new MultiResolver(new InMemoryProfileResolver(goalProfile1, goalProfile2), packageResolver);
            
            // Wrap in SnapshotSource to generate snapshots for differential-only profiles
            var profileSource = new SnapshotSource(combinedResolver);

            var terminologyService = new LocalTerminologyService(profileSource.AsAsync());
            var validator = new Validator(profileSource.AsAsync(), terminologyService);

            // Act
            var outcome = validator.Validate(carePlan);

            // Assert
            // Filter out informational or warning messages, focus on errors
            var errors = outcome.Issue.Where(issue => 
                issue.Severity == OperationOutcome.IssueSeverity.Error || 
                issue.Severity == OperationOutcome.IssueSeverity.Fatal).ToList();

            errors.Should().BeEmpty(
                "because both contained Goals are valid and should not generate errors");
        }
    }
}
