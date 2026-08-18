/*
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Firely.Fhir.Validation.Tests
{
    [TestClass]
    public class KeyedObjectValidatorTests
    {
        // A keyed object's entries are the flattened children of the container node - CodeableConcept
        // with N codings and a text stands in for a JSON object with N+1 properties here.
        private static PocoNode entries(int codings, bool withText = false) =>
            new CodeableConcept
            {
                Coding = [.. Enumerable.Range(0, codings).Select(i => new Coding("http://example.org", $"c{i}"))],
                Text = withText ? "text" : null
            }.ToPocoNode();

        private static ResultReport validate(KeyedObjectValidator sut, PocoNode input) =>
            ((IValidatable)sut).Validate(input, ValidationSettings.BuildMinimalContext(), new ValidationState());

        [TestMethod]
        public void EntriesAreCountedAgainstCardinality()
        {
            var sut = new KeyedObjectValidator(ResultAssertion.SUCCESS, min: 1, max: 2);

            // The container itself is a single node, so without entry counting an empty object would
            // wrongly satisfy min=1 and a three-entry object would wrongly satisfy max=2.
            Assert.IsFalse(validate(sut, entries(0)).IsSuccessful);
            Assert.IsTrue(validate(sut, entries(1)).IsSuccessful);
            Assert.IsTrue(validate(sut, entries(2)).IsSuccessful);
            Assert.IsFalse(validate(sut, entries(2, withText: true)).IsSuccessful);
        }

        [TestMethod]
        public void NoCardinalityMeansNoCountConstraint()
        {
            var sut = new KeyedObjectValidator(ResultAssertion.SUCCESS);

            Assert.IsTrue(validate(sut, entries(0)).IsSuccessful);
            Assert.IsTrue(validate(sut, entries(5)).IsSuccessful);
        }

        [TestMethod]
        public void EveryEntryIsValidatedAgainstTheEntryAssertion()
        {
            var sut = new KeyedObjectValidator(new FhirTypeLabelValidator("Quantity"));

            var result = validate(sut, entries(2));
            Assert.IsFalse(result.IsSuccessful);
            Assert.AreEqual(2, result.Evidence.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(IncorrectElementDefinitionException))]
        public void UpperCardinalityCannotBeLowerThanLower()
        {
            _ = new KeyedObjectValidator(ResultAssertion.SUCCESS, min: 2, max: 1);
        }

        [TestMethod]
        [ExpectedException(typeof(IncorrectElementDefinitionException))]
        public void CardinalityCannotBeNegative()
        {
            _ = new KeyedObjectValidator(ResultAssertion.SUCCESS, min: -1);
        }
    }
}
