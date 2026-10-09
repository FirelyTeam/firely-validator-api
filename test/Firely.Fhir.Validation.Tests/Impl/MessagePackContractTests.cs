/*
 * Copyright (c) 2026, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://github.com/FirelyTeam/firely-validator-api/blob/main/LICENSE
 */

using FluentAssertions;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Firely.Fhir.Validation.Tests
{
    /// <summary>
    /// Firely CAR serializes compiled schemas with MessagePack's DynamicObjectResolverAllowPrivate, which
    /// deserializes a [DataContract] type through a single constructor it picks by reflection. This test
    /// mirrors that pick (MessagePack.Internal.ObjectSerializationInfo, 3.1.x), so a type that would
    /// round-trip through the wrong constructor fails here instead of in CAR.
    /// </summary>
    [TestClass]
    public class MessagePackContractTests
    {
        private const BindingFlags DECLARED_INSTANCE = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        [TestMethod]
        public void AssertionTypesHaveAnUnambiguousSerializationConstructor()
        {
            var types = contractTypes().ToList();
            types.Should().Contain(typeof(ReferencedInstanceValidator.TargetCase));

            var problems = types.SelectMany(checkType).ToList();
            problems.Should().BeEmpty();
        }

        private static IEnumerable<Type> contractTypes()
        {
            var assertions = typeof(IAssertion).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAssertion).IsAssignableFrom(t) && isContract(t));

            // Follow the members to pick up companion types such as ReferencedInstanceValidator.TargetCase.
            var found = new HashSet<Type>();
            var todo = new Stack<Type>(assertions);
            while (todo.TryPop(out var type))
            {
                if (!found.Add(type)) continue;
                foreach (var member in serializedMembers(type))
                    foreach (var t in new[] { member.Type }.Concat(member.Type.IsArray ? [member.Type.GetElementType()!] : member.Type.GetGenericArguments()))
                        if (t.IsClass && !t.IsAbstract && isContract(t)) todo.Push(t);
            }
            return found;
        }

        private static bool isContract(Type t) => t.GetCustomAttribute<DataContractAttribute>() is not null;

        private record Member(string Name, Type Type, bool Settable);

        private static IEnumerable<Member> serializedMembers(Type type)
        {
            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var p in t.GetProperties(DECLARED_INSTANCE))
                    if (p.GetCustomAttribute<DataMemberAttribute>() is { } dm)
                        yield return new(dm.Name ?? p.Name, p.PropertyType, p.SetMethod is not null);
                foreach (var f in t.GetFields(DECLARED_INSTANCE))
                    if (f.GetCustomAttribute<DataMemberAttribute>() is { } dm)
                        yield return new(dm.Name ?? f.Name, f.FieldType, !f.IsInitOnly);
            }
        }

        private static IEnumerable<string> checkType(Type type)
        {
            var members = serializedMembers(type).ToList();
            var ctors = type.GetConstructors(DECLARED_INSTANCE);

            bool matches(ConstructorInfo c) => c.GetParameters().All(p => members.Any(m =>
                string.Equals(m.Name, p.Name, StringComparison.OrdinalIgnoreCase) &&
                (p.ParameterType == m.Type || p.ParameterType.IsAssignableFrom(m.Type))));

            var chosen = ctors.Where(c => c.GetCustomAttribute<SerializationConstructorAttribute>() is not null).ToList();
            if (chosen.Count == 0)
            {
                var top = ctors.Where(matches).GroupBy(c => c.GetParameters().Length).MaxBy(g => g.Key);
                if (top is null)
                {
                    yield return $"{type}: no constructor whose parameters all match a [DataMember].";
                    yield break;
                }
                chosen = top.ToList();
            }

            if (chosen.Count > 1)
            {
                yield return $"{type}: ambiguous constructors {string.Join(" and ", chosen.Select(c => $"({string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})"))}.";
                yield break;
            }

            var ctorParams = chosen[0].GetParameters().Select(p => p.Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var m in members.Where(m => !ctorParams.Contains(m.Name) && !m.Settable))
                yield return $"{type}: [DataMember] {m.Name} is neither a constructor parameter nor settable.";
        }
    }
}
