/* 
 * Copyright (c) 2024, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Firely.Fhir.Validation.Tests
{
    public abstract class BasicValidatorDataAttribute : Attribute, ITestDataSource
    {
        public abstract IEnumerable<object?[]> GetData();

        public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
            => GetData();

        public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
        {
            return data is not null ? string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data)) : null;
        }
    }
}
