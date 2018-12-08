﻿using System.Reflection.Emit;
using ILLightenComparer.Emit.Emitters;

namespace ILLightenComparer.Emit.Extensions
{
    internal static class MethodBuilderExtensions
    {
        public static ILEmitter CreateILEmitter(this MethodBuilder methodBuilder)
        {
#if DEBUG
            return new ILEmitter($"\n{methodBuilder.DisplayName()}", methodBuilder.GetILGenerator());
#else
            return new ILEmitter(methodBuilder.GetILGenerator());
#endif
        }

        public static ILEmitter CreateILEmitter(this ConstructorBuilder constructorBuilder)
        {
#if DEBUG
            return new ILEmitter($"\n{constructorBuilder.DisplayName()}", constructorBuilder.GetILGenerator());
#else
            return new ILEmitter(constructorBuilder.GetILGenerator());
#endif
        }
    }
}