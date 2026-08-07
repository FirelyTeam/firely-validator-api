/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class ValidationSettingsCopyTests
    {
        [TestMethod]
        public void CopyGetsItsOwnFilterCollections()
        {
            var original = ValidationSettings.BuildMinimalContext();
            var originalExcludes = original.ExcludeFilters.Count;

            var copy = original with { };
            copy.ExcludeFilters.Add(a => a is FhirPathValidator);
            copy.IncludeFilters.Add(_ => true);

            Assert.AreEqual(originalExcludes, original.ExcludeFilters.Count);
            Assert.AreEqual(0, original.IncludeFilters.Count);
            Assert.AreEqual(originalExcludes + 1, copy.ExcludeFilters.Count);
        }

        [TestMethod]
        public void CopySharesServicesAndDelegates()
        {
            var original = ValidationSettings.BuildMinimalContext();
            original.TransformIssues = issue => issue;
            original.SelectValidationProfiles = (_, profiles, _, _) => profiles;

            var copy = original with { };

            Assert.AreSame(original.ValidateCodeService, copy.ValidateCodeService);
            Assert.AreSame(original.ElementSchemaResolver, copy.ElementSchemaResolver);
            Assert.AreSame(original.TransformIssues, copy.TransformIssues);
            Assert.AreSame(original.SelectValidationProfiles, copy.SelectValidationProfiles);
            Assert.AreEqual(original.ConstraintBestPractices, copy.ConstraintBestPractices);
        }

        [TestMethod]
        public void MutatingTheCopyDoesNotAffectTheOriginal()
        {
            var original = ValidationSettings.BuildMinimalContext();

            var copy = original with { };
            copy.TransformIssues = _ => null;
            copy.ModelInspector = Hl7.Fhir.Introspection.ModelInspector.Base;

            Assert.IsNull(original.TransformIssues);
            Assert.IsNull(original.ModelInspector);
        }
    }
}
