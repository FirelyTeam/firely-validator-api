﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MessagePack.Formatters
{
    internal class SimpleTypedElement : ITypedElement
    {
        public SimpleTypedElement(string name) => Name = name;

        public string Name { get; set; }

        public string? InstanceType { get; set; }

        public object? Value { get; set; }

        public string? Location { get; set; }

        IElementDefinitionSummary? ITypedElement.Definition => null;

        public IEnumerable<ITypedElement>? Children { get; set; }

        IEnumerable<ITypedElement> ITypedElement.Children(string? name = null)
        {
            var children = Children ?? Enumerable.Empty<ITypedElement>();
            return name is null ? children : children.Where(c => c.Name.MatchesPrefix(name)).ToList();
        }
    }


    public class TypedElementFormatter : IMessagePackFormatter<ITypedElement?>
    {
        public static readonly IMessagePackFormatter<ITypedElement?> Instance = new TypedElementFormatter();

        private TypedElementFormatter()
        {
        }

        public ITypedElement? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil()) return null;

            var stringFormatter = options.Resolver.GetFormatterWithVerify<string>();
            var valueFormatter = TypedElementValueFormatter.Instance;

            if (reader.ReadArrayHeader() != 5) throw new InvalidOperationException("Incorrect binary format: expected exactly 5 elements in serialized ITypedElement.");

            var name = stringFormatter.Deserialize(ref reader, options);
            var result = new SimpleTypedElement(name)
            {
                InstanceType = stringFormatter.Deserialize(ref reader, options),
                Value = valueFormatter.Deserialize(ref reader, options),
                Location = stringFormatter.Deserialize(ref reader, options)
            };

            var childCount = reader.ReadArrayHeader();
            options.Security.DepthStep(ref reader);

            var children = new List<ITypedElement>();

            try
            {
                for (var childIndex = 0; childIndex < childCount; childIndex++)
                {
                    var child = Deserialize(ref reader, options);
                    if (child is not null) children.Add(child);
                }

                result.Children = children;
            }
            finally
            {
                reader.Depth--;
            }

            return result;
        }

        public void Serialize(ref MessagePackWriter writer, ITypedElement? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            var stringFormatter = options.Resolver.GetFormatterWithVerify<string>();
            var valueFormatter = TypedElementValueFormatter.Instance;

            // The ITypedElement is serialized as an array of 5 items: Name, InstanceType, etc.
            writer.WriteArrayHeader(5);
            stringFormatter.Serialize(ref writer, value!.Name, options);
            stringFormatter.Serialize(ref writer, value.InstanceType, options);
            valueFormatter.Serialize(ref writer, value.Value, options);
            stringFormatter.Serialize(ref writer, value.Location, options);

            var children = value.Children().ToList();
            writer.WriteArrayHeader(children.Count);
            foreach (var child in children)
            {
                Serialize(ref writer, child, options);
            }
        }
    }
}
