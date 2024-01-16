/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

namespace Firely.Fhir.Validation
{
    /// <summary>
    /// An interface implemented by validators and assertions that will always report a given fixed result, independent of input.
    /// </summary>
    /// <remarks>These are validators like <see cref="IssueAssertion"/>, <see cref="TraceAssertion"/> and <see cref="ResultAssertion"/> that have
    /// the result of running them in a validation configured at compilation time. It is used to simplify schema's at compilation time.</remarks>
    internal interface IFixedResult
    {
        /// <summary>
        /// The <see cref="ValidationResult"/> represented by this instance.
        /// </summary>
        ValidationResult FixedResult { get; }
    }

}