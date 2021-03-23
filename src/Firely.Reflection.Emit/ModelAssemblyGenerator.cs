﻿/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Validation;
using Lokad.ILPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Firely.Reflection.Emit
{
    public class ModelAssemblyGenerator
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly UnionTypeGenerator _unionTypeGen;
        private readonly Dictionary<string, Type> _typesUnderConstruction = new();

        public Func<string, string> TypeNameToCanonical { get; }
        public Func<string, Type?> ResolveToType { get; }
        public Func<string, Task<ISourceNode?>> ResolveToSourceNode { get; }


        public ModelAssemblyGenerator(string assemblyName, Func<string, string> typeNameToCanonical, Func<string, Type?> resolveToType, Func<string, Task<ISourceNode?>> resolveToSourceNode)
        {
            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentException($"'{nameof(assemblyName)}' cannot be null or empty.", nameof(assemblyName));

            AssemblyName fullAssemblyName = new(assemblyName);
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    fullAssemblyName,
                    AssemblyBuilderAccess.Run);

            // For a single-module assembly, the module name is usually
            // the assembly name plus an extension.
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(assemblyName);

            _unionTypeGen = new UnionTypeGenerator(_moduleBuilder);

            TypeNameToCanonical = typeNameToCanonical ?? throw new ArgumentNullException(nameof(typeNameToCanonical));
            ResolveToType = resolveToType ?? throw new ArgumentNullException(nameof(resolveToType));
            ResolveToSourceNode = resolveToSourceNode ?? throw new ArgumentNullException(nameof(resolveToSourceNode));
        }

        public async Task AddType(string canonicalOrTypeName) => await getTypeBuilder(canonicalOrTypeName);

        private async Task<Type> getTypeBuilder(string canonicalOrTypeName)
        {
            bool isTypeName = !canonicalOrTypeName.StartsWith("http://");

            // if this is a typename, first search in the types under construction by type name.
            if (isTypeName)
            {
                var foundType = _typesUnderConstruction.Values.SingleOrDefault(type => type.Name == canonicalOrTypeName);
                if (foundType is not null) return foundType;
            }

            // not a known local type (yet). make sure we're dealing with canonicals from now on to lookup the type.
            var canonical = isTypeName ? TypeNameToCanonical(canonicalOrTypeName) : canonicalOrTypeName;

            // lookup the types under construction by canonical.
            if (_typesUnderConstruction.TryGetValue(canonical, out Type t)) return t;

            // Ok, we've not yet constructed this type. Before constructing a new type,
            // ask the environment whether this type is known externally, so we don't have
            // to generate it.
            var type = ResolveToType(canonical);

            if (type is not null)
            {
                // Add this external type as if we had constructed it, which
                // saves a lookup to the environment next time.
                _typesUnderConstruction.Add(canonical, type);
                return type;
            }

            // Unknown, so we'll have to go out and generate it.
            var node = await ResolveToSourceNode(canonical);
            if (node is null) throw new InvalidOperationException($"Cannot generate type for canonical {canonical}, since it cannot be resolved to a StructureDefinition.");
            var sdInfo = StructureDefinitionInfo.FromStructureDefinition(node);

            // This function will add the new function to the _typedUnderConstruction.
            // Not completely SRP, but by the time this function finishes, it's too
            // late to add it to avoid circular references.
            return await AddFromStructureDefinition(sdInfo);
        }

        internal async Task<Type> AddFromStructureDefinition(StructureDefinitionInfo sdInfo)
        {
            var newTypeAttributes = TypeAttributes.Public;
            if (sdInfo.IsAbstract) newTypeAttributes |= TypeAttributes.Abstract;

            var baseType = sdInfo.BaseCanonical is not null ? await getTypeBuilder(sdInfo.BaseCanonical) : null;

            // Generating our base class can - in the meantime - have generated "us", so we can just return
            // before actually creating us as a new type.
            if (_typesUnderConstruction.TryGetValue(sdInfo.Canonical, out Type t)) return t;

            var newType = _moduleBuilder.DefineType(sdInfo.TypeName, newTypeAttributes, parent: baseType);
            _typesUnderConstruction.Add(sdInfo.Canonical, newType);

            // Add FhirType(<name>, IsResource=<>) attribute
            var constructor = typeof(FhirTypeAttribute).GetConstructor(new[] { typeof(string) });
            var isResourceProp = typeof(FhirTypeAttribute).GetProperty(nameof(FhirTypeAttribute.IsResource));
            CustomAttributeBuilder attrBuilder = new(constructor, new object[] { sdInfo.TypeName }, new PropertyInfo[] { isResourceProp }, new object[] { sdInfo.IsResource });
            newType.SetCustomAttribute(attrBuilder);

            // Add each element
            if (sdInfo.Elements.Any())
                await addElements(newType, sdInfo.Elements);

            return newType;
        }

        private async Task addElements(TypeBuilder newType, IEnumerable<ElementDefinitionInfo> elementNodes)
        {
            foreach (var elementNode in elementNodes)
            {
                // Don't generate properties that have been removed
                if (elementNode.Max is not null && elementNode.Max == "0") continue;

                await emitProperty(newType, elementNode);
            }
        }

        private async Task<PropertyBuilder> emitProperty(TypeBuilder newType, ElementDefinitionInfo element)
        {
            Type memberType;

            if (element.Backbone is not null)
            {
                memberType = await AddFromStructureDefinition(element.Backbone);
            }
            else
            {
                if ((element.TypeRef?.Length ?? 0) == 0) throw new InvalidOperationException("Encountered an element definition without typerefs: " + element);

                Type[] memberTypes = await Task.WhenAll(
                    element.TypeRef.Select(tr =>
                    getTypeBuilder(tr.Type)));

                //memberType = deriveCommonBase(memberTypes) ??
                //    throw new NotSupportedException($"Cannot find a common baseclass for the choice element {element}.");
                if (memberTypes.Length > 1)
                    memberType = _unionTypeGen.CreateUnionType(memberTypes);
                else
                    memberType = memberTypes.Single();

                if (element.IsCollection) memberType = typeof(List<>).MakeGenericType(memberType);
            }

            PropertyBuilder newProperty = newType.DefineProperty(
                element.Name, PropertyAttributes.None, memberType, null);

            MethodAttributes getSetAttr = MethodAttributes.Public |
                MethodAttributes.SpecialName | MethodAttributes.HideBySig;

            // Define the "get" accessor method for Number. The method returns
            // an integer and has no arguments. (Note that null could be
            // used instead of Types.EmptyTypes)
            MethodBuilder newPropertyGetAccessor = newType.DefineMethod(
                $"get_{element.Name}",
                getSetAttr,
                memberType,
                Type.EmptyTypes);

            // Just make the getter throw NotImplemented
            ILGenerator numberGetIL = newPropertyGetAccessor.GetILGenerator();
            numberGetIL.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Array.Empty<Type>()));
            numberGetIL.Emit(OpCodes.Throw);

            newProperty.SetGetMethod(newPropertyGetAccessor);

            newProperty.SetCustomAttribute(createFhirElementAttribute(element));
            if (element.IsCollection) newProperty.SetCustomAttribute(createCardinalityAttribute(element));

            return newProperty;
        }

        private static CustomAttributeBuilder createCardinalityAttribute(ElementDefinitionInfo element)
        {
            var aT = typeof(CardinalityAttribute);
            var constructor = aT.GetConstructor(Array.Empty<Type>());

            int min = element.Min ?? 0;
            int max = element.Max == "*" ? -1 : element.Max is { } ? int.Parse(element.Max) : 1;
            var props = new[] { aT.GetProperty(nameof(CardinalityAttribute.Min)),
                                aT.GetProperty(nameof(CardinalityAttribute.Max)) };

            return new(constructor, Array.Empty<object>(), props, new object[] { min, max });
        }

        private static CustomAttributeBuilder createFhirElementAttribute(ElementDefinitionInfo element)
        {
            var feat = typeof(FhirElementAttribute);
            var constructor = feat.GetConstructor(new[] { typeof(string) });
            var props = new[] { feat.GetProperty(nameof(FhirElementAttribute.Choice)),
                                feat.GetProperty(nameof(FhirElementAttribute.IsPrimitiveValue)),
                                feat.GetProperty(nameof(FhirElementAttribute.XmlSerialization)),
                                feat.GetProperty(nameof(FhirElementAttribute.Order)),
                                feat.GetProperty(nameof(FhirElementAttribute.InSummary)) };

            var isResourceElement = element.TypeRef?.First().IsResourceType == true;

            var choiceType = element.IsChoice switch
            {
                true when isResourceElement => ChoiceType.ResourceChoice,
                true when !isResourceElement => ChoiceType.DatatypeChoice,
                _ => ChoiceType.None
            };

            return new(constructor, new object[] { element.Name },
                            props, new object[] { choiceType, element.IsPrimitiveValue, element.Representation, element.Order, element.InSummary });
        }

        private Type? deriveCommonBase(Type[] memberTypes)
        {
            if (memberTypes.Length == 1) return memberTypes[0];

            // The list of ancestors of the (randomly chosen) first
            // of the member types.
            // Note that the closest ancestors are at the beginning
            // of the list. We'll prefer a common base that is as
            // close to the memberTypes as possible.
            var candidates = allBases(memberTypes[0]);
            foreach (var memberType in memberTypes[1..])
                candidates = candidates.Where(c => c.IsAssignableFrom(memberType)).ToList();

            return candidates.FirstOrDefault();

            static List<Type> allBases(Type parent)
            {
                List<Type> bases = new();
                var current = parent.BaseType;
                while (current is not null)
                {
                    bases.Add(current);
                    current = current.BaseType;
                }

                return bases;
            }
        }

        public void FinalizeTypes()
        {
            var keys = _typesUnderConstruction.Keys.ToList();

            foreach (var key in keys)
            {
                if (_typesUnderConstruction[key] is TypeBuilder builder && !builder.IsCreated())
                    _typesUnderConstruction[key] = builder.CreateType()!;
            }
        }

        public Assembly GetAssembly()
        {
            FinalizeTypes();
            return _assemblyBuilder;
        }

        public void WriteToDll(string path)
        {
            FinalizeTypes();

            // The following line saves the single-module assembly. This
            // requires AssemblyBuilderAccess to include Save. You can now
            // type "ildasm MyDynamicAsm.dll" at the command prompt, and
            // examine the assembly. You can also write a program that has
            // a reference to the assembly, and use the MyDynamicType type.
            //
            var generator = new AssemblyGenerator();
            generator.GenerateAssembly(_assemblyBuilder, path);
        }

        public byte[] GetAssemblyBytes()
        {
            FinalizeTypes();

            // The following line saves the single-module assembly. This
            // requires AssemblyBuilderAccess to include Save. You can now
            // type "ildasm MyDynamicAsm.dll" at the command prompt, and
            // examine the assembly. You can also write a program that has
            // a reference to the assembly, and use the MyDynamicType type.
            //
            var generator = new AssemblyGenerator();
            return generator.GenerateAssemblyBytes(_assemblyBuilder);
        }

    }

    internal class UnionTypeGenerator
    {
        private readonly Dictionary<int, Type> _unionTypes = new();

        public UnionTypeGenerator(ModuleBuilder targetModule)
        {
            TargetModule = targetModule;
        }

        public Type CreateUnionType(Type[] memberTypes)
        {
            var union = GetOpenUnion(memberTypes.Length);
            return union.MakeGenericType(memberTypes);
        }

        public Type GetOpenUnion(int numArguments)
        {
            if (_unionTypes.TryGetValue(numArguments, out var type)) return type;

            var newTypeBuilder = TargetModule.DefineType($"Unions.UnionType_{numArguments}",
                TypeAttributes.Public | TypeAttributes.Abstract);

            var genericArgList = Enumerable.Range(1, numArguments).Select(i => "T" + i).ToArray();
            _ = newTypeBuilder.DefineGenericParameters(genericArgList);

            var newType = newTypeBuilder.CreateType();
            _unionTypes.Add(numArguments, newType);

            return newType;
        }


        public ModuleBuilder TargetModule { get; }
    }

}
