/* 
 * Copyright (c) 2021, Firely (info@fire.ly) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-net-sdk/master/LICENSE
 */

using System;
using System.Globalization;
using System.Reflection;

namespace Firely.Reflection.Emit
{
    public class TypeWrapper : Type
    {
        public TypeWrapper(Type wrapped) => Wrapped = wrapped;

        public Type Wrapped { get; }

        public override Assembly Assembly => Wrapped.Assembly;

        public override string AssemblyQualifiedName => Wrapped.AssemblyQualifiedName;

        public override Type BaseType => Wrapped.BaseType;

        public override string FullName => Wrapped.FullName;

        public override Guid GUID => Wrapped.GUID;

        public override Module Module => Wrapped.Module;

        public override string Namespace => Wrapped.Namespace;

        public override Type UnderlyingSystemType => Wrapped.UnderlyingSystemType;

        public override string Name => Wrapped.Name;

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => Wrapped.GetConstructors(bindingAttr);
        public override object[] GetCustomAttributes(bool inherit) => Wrapped.GetCustomAttributes(inherit);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Wrapped.GetCustomAttributes(attributeType, inherit);
        public override Type GetElementType() => Wrapped.GetElementType();
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => Wrapped.GetEvent(name, bindingAttr);
        public override EventInfo[] GetEvents(BindingFlags bindingAttr) => Wrapped.GetEvents(bindingAttr);
        public override FieldInfo GetField(string name, BindingFlags bindingAttr) => Wrapped.GetField(name, bindingAttr);
        public override FieldInfo[] GetFields(BindingFlags bindingAttr) => Wrapped.GetFields(bindingAttr);
        public override Type GetInterface(string name, bool ignoreCase) => Wrapped.GetInterface(name, ignoreCase);
        public override Type[] GetInterfaces() => Wrapped.GetInterfaces();
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => Wrapped.GetMembers(bindingAttr);
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => Wrapped.GetMethods(bindingAttr);
        public override Type GetNestedType(string name, BindingFlags bindingAttr) => Wrapped.GetNestedType(name, bindingAttr);
        public override Type[] GetNestedTypes(BindingFlags bindingAttr) => Wrapped.GetNestedTypes(bindingAttr);
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => Wrapped.GetProperties(bindingAttr);
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => Wrapped.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        public override bool IsDefined(Type attributeType, bool inherit) => Wrapped.IsDefined(attributeType, inherit);

        public override bool Equals(object o) => Wrapped.Equals(o);
        public override bool Equals(Type o) => Wrapped.Equals(o);
        public override int GetHashCode() => Wrapped.GetHashCode();
        protected override TypeAttributes GetAttributeFlagsImpl() => Wrapped.Attributes;
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) =>
            Wrapped.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) =>
            Wrapped.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) =>
            Wrapped.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);

        protected override bool HasElementTypeImpl() => Wrapped.HasElementType;
        protected override bool IsArrayImpl() => Wrapped.IsArray;
        protected override bool IsByRefImpl() => Wrapped.IsByRef;
        protected override bool IsCOMObjectImpl() => Wrapped.IsCOMObject;
        protected override bool IsPointerImpl() => Wrapped.IsPointer;
        protected override bool IsPrimitiveImpl() => Wrapped.IsPrimitive;
    }

}
