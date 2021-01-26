using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Firely.Fhir.Validation.Tests
{
    public abstract class SimpleAssertionDataAttribute : Attribute, ITestDataSource
    {
        public abstract IEnumerable<object[]> GetData();

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
            => GetData();

        public string? GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return data != null ? string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data)) : null;
        }
    }
}
