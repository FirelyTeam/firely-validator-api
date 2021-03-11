﻿/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using Hl7.Fhir.ElementModel;
using Hl7.Fhir.Introspection;
using Lokad.ILPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Firely.Reflection.Emit
{
    public class AssemblyComposer
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<string, Type> _typesUnderConstruction = new();

        public Func<string, string> TypeNameToCanonical { get; }
        public Func<string, Task<ISourceNode>> ResolveCanonical { get; }

        public AssemblyComposer(string assemblyName, Func<string, string> typeNameToCanonical, Func<string, Task<ISourceNode>> resolveCanonical)
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
            TypeNameToCanonical = typeNameToCanonical ?? throw new ArgumentNullException(nameof(typeNameToCanonical));
            ResolveCanonical = resolveCanonical ?? throw new ArgumentNullException(nameof(resolveCanonical));
        }

        public async Task<Type> GetType(string canonicalOrTypeName)
        {
            var canonical = canonicalOrTypeName.StartsWith("http://") ? canonicalOrTypeName : TypeNameToCanonical(canonicalOrTypeName);

            if (_typesUnderConstruction.TryGetValue(canonical, out Type t)) return t;

            var node = await ResolveCanonical(canonical);

            // This function will add the new function to the _typedUnderConstruction.
            // Not completely SRP, but by the time this function finishes, it's too
            // late to add it to avoid circular references.
            var sdInfo = StructureDefinitionInfo.FromStructureDefinition(node);
            return await AddFromStructureDefinition(sdInfo);
        }

        internal async Task<Type> AddFromStructureDefinition(StructureDefinitionInfo sdInfo)
        {
            var newTypeAttributes = TypeAttributes.Public;
            if (sdInfo.IsAbstract) newTypeAttributes |= TypeAttributes.Abstract;

            var baseType = sdInfo.BaseCanonical is not null ? await GetType(sdInfo.BaseCanonical) : null;

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

        private async Task addElements(TypeBuilder newType, ElementDefinitionInfo[] elementNodes)
        {
            int? pathLength = elementNodes.FirstOrDefault()?.PathParts.Length;

            foreach (var elementNode in elementNodes)
            {
                bool stillToDo = elementNode.IsBackboneElement || elementNode.IsContentReference || elementNode.IsPrimitive || elementNode.TypeRef?.Length != 1;

                if (elementNode.PathParts.Length == pathLength && !stillToDo)
                    await emitProperty(newType, elementNode);
            }
        }

        private async Task<PropertyBuilder> emitProperty(TypeBuilder newType, ElementDefinitionInfo element)
        {
            var memberType = await GetType(element.TypeRef.Single().Type);

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
            numberGetIL.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetTypeInfo().DeclaredConstructors.First(x => x.GetParameters().Length == 0));
            numberGetIL.Emit(OpCodes.Throw);

            newProperty.SetGetMethod(newPropertyGetAccessor);

            return newProperty;
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
    }
}
