/* 
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using P = Hl7.Fhir.ElementModel.Types;

namespace Firely.Reflection.Emit
{
    /// <summary>
    /// Generates a .DLL or in-memory dynamic DLL that represents the contents
    /// of a (non-profile) StructureDefinition.
    /// </summary>
    public class ModelAssemblyGenerator
    {
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly UnionTypeGenerator _unionTypeGen;
        private readonly string _namespace;

        // A map that maps canonicals to generated types (or builders).
        private readonly Dictionary<string, Type> _typesUnderConstruction = new();

        /// <summary>
        /// External mapper that knows how to translate a typename to its canonical.
        /// </summary>
        /// <remarks>Defaults to a mapper that assumes the type name is from the
        /// FHIR core specfication.</remarks>
        public Func<string, string> TypeNameToCanonical { get; } = nameToCanonical;

        private static string nameToCanonical(string name) => "http://hl7.org/fhir/StructureDefinition/" + name;

        /// <summary>
        /// External mapper that knows how to translate a canonical to a .NET type
        /// </summary>
        /// <remarks>Defaults <see cref="ResolveFhirPathPrimitiveToType(string)"/>.</remarks>
        public Func<string, Type?> ResolveToType { get; } = ResolveFhirPathPrimitiveToType;

        /// <summary>
        /// This resolves a FhirPrimitive (canonical starting with http://hl7.org/fhirpath) to
        /// its implementation in Hl7.Fhir.ElementModel.Types and is used as the default resolver
        /// installed in <see cref="ResolveToType"/>.
        /// </summary>
        public static Type? ResolveFhirPathPrimitiveToType(string canonical)
        {
            const string SYSTEMTYPEPREFIX = "http://hl7.org/fhirpath/System.";

            if (canonical.StartsWith(SYSTEMTYPEPREFIX))
            {
                var systemTypeName = canonical[SYSTEMTYPEPREFIX.Length..];
                if (P.Any.TryGetSystemTypeByName(systemTypeName, out Type? sys)) return sys;
            }

            return null;
        }


        /// <summary>
        /// External resolver that can fetch a StructureDefinition in SourceNode form
        /// using its canonical.
        /// </summary>
        public Func<string, Task<ISourceNode?>> ResolveToSourceNode { get; }

        /// <summary>
        /// Initializes a ModelAssemblyGenerator.
        /// </summary>
        /// <param name="assemblyName">Name given to the generated assembly.</param>
        /// <param name="ns">Namespace to use for the generated classes.</param>
        /// <param name="resolveToSourceNode">A function resolves a canonical to a parsed StructureDefinition.</param>
        public ModelAssemblyGenerator(
            string assemblyName,
            string ns,
            Func<string, Task<ISourceNode?>> resolveToSourceNode)
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

            _namespace = ns;

            ResolveToSourceNode = resolveToSourceNode ?? throw new ArgumentNullException(nameof(resolveToSourceNode));
        }

        /// <summary>
        /// Initializes a ModelAssemblyGenerator.
        /// </summary>
        /// <param name="assemblyName">Name given to the generated assembly.</param>
        /// <param name="ns">Namespace to use for the generated classes.</param>
        /// <param name="typeNameToCanonical">A function that turns a type name into the canonical for the StructureDefinition of that type.</param>
        /// <param name="resolveToSourceNode">A function resolves a canonical to a parsed StructureDefinition.</param>
        /// <param name="resolveToType">A function that resolves a canonical to an already existing type.</param>
        public ModelAssemblyGenerator(string assemblyName, string ns, Func<string, string> typeNameToCanonical, Func<string, Type?> resolveToType, Func<string, Task<ISourceNode?>> resolveToSourceNode)
            : this(assemblyName, ns, resolveToSourceNode)
        {
            TypeNameToCanonical = typeNameToCanonical ?? throw new ArgumentNullException(nameof(typeNameToCanonical));
            ResolveToType = resolveToType ?? throw new ArgumentNullException(nameof(resolveToType));
        }

        /// <summary>
        /// Adds the given (FHIR) type to the dynamic assembly.
        /// </summary>
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

            // Avoid using the bogus inheritance relationship introduced between primitive datatypes
            // (e.g. id/string) in FHIR.
            while (baseType != null && baseType.IsAbstract == false) baseType = baseType.BaseType;

            // Generating our base class can - in the meantime - have generated "us", so we can just return
            // before actually creating us as a new type.
            if (_typesUnderConstruction.TryGetValue(sdInfo.Canonical, out Type t)) return t;

            var newType = _moduleBuilder.DefineType(_namespace + "." + sdInfo.TypeName, newTypeAttributes, parent: baseType);
            _typesUnderConstruction.Add(sdInfo.Canonical, newType);

            // Add FhirType(<name>, IsResource=<>, IsNestedType=<>) attribute
            newType.SetCustomAttribute(
                buildCustomAttribute<FhirTypeAttribute>(new[] { sdInfo.TypeName },
                    new()
                    {
                        [nameof(FhirTypeAttribute.IsResource)] = sdInfo.Kind == StructureDefinitionKind.Resource,
                        [nameof(FhirTypeAttribute.IsNestedType)] = sdInfo.Kind == StructureDefinitionKind.Backbone
                    }));

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
            Type[] choices = Array.Empty<Type>();

            if (element.Backbone is not null)
            {
                memberType = await AddFromStructureDefinition(element.Backbone);
            }
            else
            {
                if ((element.TypeRef?.Length ?? 0) == 0) throw new InvalidOperationException("Encountered an element definition without typerefs: " + element);

                choices = await Task.WhenAll(
                    element.TypeRef.Select(tr =>
                    getTypeBuilder(tr.Type)));

                //memberType = deriveCommonBase(memberTypes) ??
                //    throw new NotSupportedException($"Cannot find a common baseclass for the choice element {element}.");
                memberType = choices.Length > 1 ?
                    _unionTypeGen.CreateUnionType(choices)
                    : choices.Single();

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

            //if (choices.Length > 1)
            //    newProperty.SetCustomAttribute(createAllowedTypesAttribute(choices));

            return newProperty;

        }


        /* Unfortunately, this won't work.  The types in the AllowedTypes attribute
           may not yet have been created, so at least you have to make sure they get
           CreateTyped() first (overwriting them in the _typesUnderconstruction).
           Even then, our 3rd party dll to help write the generated dll to disk will then
           throw:

           System.IO.FileNotFoundException: Could not load file or assembly 'TestTypesAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
           Stack Trace: 
            RuntimeTypeHandle.GetTypeByNameUsingCARules(String name, QCallModule scope, ObjectHandleOnStack type)
            RuntimeTypeHandle.GetTypeByNameUsingCARules(String name, RuntimeModule scope)
            CustomAttributeTypedArgument.ResolveType(RuntimeModule scope, String typeName)
            CustomAttributeTypedArgument.ctor(RuntimeModule scope, CustomAttributeEncodedArgument encodedArg)
            CustomAttributeTypedArgument.ctor(RuntimeModule scope, CustomAttributeEncodedArgument encodedArg)
            CustomAttributeData.get_ConstructorArguments()
            AssemblyGenerator.EncodeFixedAttributes(FixedArgumentsEncoder fa, CustomAttributeData attr)
            <>c__DisplayClass0_0.<CreateCustomAttributes>b__0(FixedArgumentsEncoder fa)
            BlobEncoder.CustomAttributeSignature(Action`1 fixedArguments, Action`1 namedArguments)
            AssemblyGenerator.CreateCustomAttributes(EntityHandle parent, IEnumerable`1 attributes)
            AssemblyGenerator.CreateProperty(PropertyInfo property, Boolean addToPropertyMap)
            AssemblyGenerator.CreatePropertiesForType(IEnumerable`1 properties)
            AssemblyGenerator.CreateType(Type type, List`1 genericParams)
            AssemblyGenerator.CreateTypes(IEnumerable`1 types, List`1 genericParams)
            AssemblyGenerator.CreateModules(IEnumerable`1 moduleInfo)
            AssemblyGenerator.GenerateAssemblyBytes(Assembly assembly, IEnumerable`1 referencedDynamicAssembly)
            AssemblyGenerator.GenerateAssembly(Assembly assembly, IEnumerable`1 referencedDynamicAssembly, String path)
            AssemblyGenerator.GenerateAssembly(Assembly assembly, String path)
            ModelAssemblyGenerator.WriteToDll(String path) line 433
        */
        /*
        private static CustomAttributeBuilder createAllowedTypesAttribute(Type[] choices)
        {
            //return buildCustomAttribute<AllowedTypesAttribute>(new[] { choices }, new());
            var attrType = typeof(AllowedTypesAttribute);
            var attrConstructor = attrType.GetConstructors().Single();

            return new CustomAttributeBuilder(attrConstructor, new[] { choices });
        }
        */

        private static CustomAttributeBuilder createCardinalityAttribute(ElementDefinitionInfo element)
        {
            int min = element.Min ?? 0;
            int max = element.Max == "*" ? -1 : element.Max is { } ? int.Parse(element.Max) : 1;

            return buildCustomAttribute<CardinalityAttribute>(
                Array.Empty<object>(),
                new()
                {
                    [nameof(CardinalityAttribute.Min)] = min,
                    [nameof(CardinalityAttribute.Max)] = max
                });
        }


        private static CustomAttributeBuilder buildCustomAttribute<T>(
            object[] constructorArg,
            Dictionary<string, object> properties)
        {
            var attrType = typeof(T);
            var attrConstructor = attrType.GetConstructor(constructorArg.Select(ca => ca.GetType()).ToArray());
            var propInfos = properties.Keys.Select(pn => attrType.GetProperty(pn)).ToArray();
            var propValues = properties.Values.ToArray();

            return new CustomAttributeBuilder(attrConstructor, constructorArg, propInfos, propValues);
        }


        private static CustomAttributeBuilder createFhirElementAttribute(ElementDefinitionInfo element)
        {
            var isResourceElement = element.TypeRef?.First().IsResourceType == true;

            var choiceType = element.IsChoice switch
            {
                true when isResourceElement => ChoiceType.ResourceChoice,
                true when !isResourceElement => ChoiceType.DatatypeChoice,
                _ => ChoiceType.None
            };

            return buildCustomAttribute<FhirElementAttribute>(
                    new[] { element.Name, choiceType, (object)element.Representation },
                    new()
                    {
                        // There's a bug in the 3rd-party lib, enum property setters in custom
                        // attributes are not emitted correctly - so we use a constructor.
                        // [nameof(FhirElementAttribute.Choice)] = choiceType,
                        // [nameof(FhirElementAttribute.XmlSerialization)] = element.Representation,
                        [nameof(FhirElementAttribute.IsPrimitiveValue)] = element.IsPrimitiveValue,
                        [nameof(FhirElementAttribute.Order)] = element.Order,
                        [nameof(FhirElementAttribute.InSummary)] = element.InSummary
                    }
                );
        }


        // Retaining this code - if we don't want to use the generated
        // faux discriminated unions to define choice types, we use this
        // function to derive a type that can be used as the common base
        // type for a choice element.
        //private static Type? deriveCommonBase(Type[] memberTypes)
        //{
        //    if (memberTypes.Length == 1) return memberTypes[0];

        //    // The list of ancestors of the (randomly chosen) first
        //    // of the member types.
        //    // Note that the closest ancestors are at the beginning
        //    // of the list. We'll prefer a common base that is as
        //    // close to the memberTypes as possible.
        //    var candidates = allBases(memberTypes[0]);
        //    foreach (var memberType in memberTypes[1..])
        //        candidates = candidates.Where(c => c.IsAssignableFrom(memberType)).ToList();

        //    return candidates.FirstOrDefault();

        //    static List<Type> allBases(Type parent)
        //    {
        //        List<Type> bases = new();
        //        var current = parent.BaseType;
        //        while (current is not null)
        //        {
        //            bases.Add(current);
        //            current = current.BaseType;
        //        }

        //        return bases;
        //    }
        //}


        private bool _finalized = false;

        /// <summary>
        /// Seals the DLL so it can be used.
        /// </summary>
        public void FinalizeTypes()
        {
            if (_finalized) return;
            _finalized = true;

            var keys = _typesUnderConstruction.Keys.ToList();

            foreach (var key in keys)
            {
                if (_typesUnderConstruction[key] is TypeBuilder builder && !builder.IsCreated())
                {
                    _typesUnderConstruction[key] = builder.CreateType()!;
                }
            }
        }

        /// <summary>
        /// Finalizes and returns the dynamically generated assembly.
        /// </summary>
        public Assembly GetAssembly()
        {
            FinalizeTypes();
            return _assemblyBuilder;
        }

        /// <summary>
        /// Finalizes the dynamically generated assembly and writes it to the given 
        /// filesystem location.
        /// </summary>
        public string WriteToDll(DirectoryInfo path)
        {
            FinalizeTypes();

            // The following line saves the single-module assembly. This
            // requires AssemblyBuilderAccess to include Save. You can now
            // type "ildasm MyDynamicAsm.dll" at the command prompt, and
            // examine the assembly. You can also write a program that has
            // a reference to the assembly, and use the MyDynamicType type.
            //
            var generator = new AssemblyGenerator();
            var outputFile = Path.Combine(path.FullName, _assemblyBuilder.GetName().Name) + ".dll";
            generator.GenerateAssembly(_assemblyBuilder, outputFile);

            return outputFile;
        }

        /// <summary>
        /// Finalizes the dynamically generated assembly and serializes it to
        /// a stream of bytes.
        /// </summary>
        /// <returns></returns>
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

}
