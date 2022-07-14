/* 
 * Copyright (C) 2021, Firely (info@fire.ly) - All Rights Reserved
 * Proprietary and confidential. Unauthorized copying of this file, 
 * via any medium is strictly prohibited.
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface implemented by validators and assertions that will always report a given fixed result, independent of input.
    /// </summary>
    /// <remarks>These are validators like <see cref="IssueAssertion"/>, <see cref="TraceAssertion"/> and <see cref="ResultAssertion"/> that have
    /// the result of running them in a validation configured at compilation time. It is used to simplify schema's at compilation time.</remarks>
    public interface IFixedResult
    {
        /// <summary>
        /// The <see cref="ValidationResult"/> represented by this instance.
        /// </summary>
        ValidationResult FixedResult { get; }

        /// <summary>
        /// Return a <see cref="ResultReport"/> with details about this fixed result.
        /// </summary>
        ResultReport AsResult();
    }

}