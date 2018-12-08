﻿using System.Reflection;
using System.Reflection.Emit;
using ILLightenComparer.Emit.Emitters;
using ILLightenComparer.Emit.Emitters.Acceptors;
using ILLightenComparer.Emit.Emitters.Members;
using ILLightenComparer.Emit.Extensions;

namespace ILLightenComparer.Emit.Members
{
    internal sealed class IntegralFieldMember : FieldMember, IArgumentsField, IIntegralAcceptor
    {
        private IntegralFieldMember(FieldInfo fieldInfo) : base(fieldInfo) { }

        public bool LoadContext => false;

        public ILEmitter LoadMembers(StackEmitter visitor, Label gotoNextMember, ILEmitter il) =>
            visitor.Visit(this, il, gotoNextMember);

        public ILEmitter Accept(CompareEmitter visitor, ILEmitter il) => visitor.Visit(this, il);

        public static IntegralFieldMember Create(MemberInfo memberInfo) =>
            memberInfo is FieldInfo info
            && info
               .FieldType
               .GetUnderlyingType()
               .IsSmallIntegral()
                ? new IntegralFieldMember(info)
                : null;
    }

    internal sealed class IntegralPropertyMember : PropertyMember, IArgumentsProperty, IIntegralAcceptor
    {
        private IntegralPropertyMember(PropertyInfo propertyInfo) : base(propertyInfo) { }

        public bool LoadContext => false;

        public ILEmitter LoadMembers(StackEmitter visitor, Label gotoNextMember, ILEmitter il) => visitor.Visit(this, il, gotoNextMember);

        public ILEmitter Accept(CompareEmitter visitor, ILEmitter il) => visitor.Visit(this, il);

        public static IntegralPropertyMember Create(MemberInfo memberInfo) =>
            memberInfo is PropertyInfo info
            && info
               .PropertyType
               .GetUnderlyingType()
               .IsSmallIntegral()
                ? new IntegralPropertyMember(info)
                : null;
    }
}