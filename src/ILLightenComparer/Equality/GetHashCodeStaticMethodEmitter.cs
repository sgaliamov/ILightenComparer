﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Extensions;
using ILLightenComparer.Shared;
using ILLightenComparer.Variables;
using Illuminator;
using Illuminator.Extensions;
using static Illuminator.Functional;

namespace ILLightenComparer.Equality
{
    internal sealed class GetHashCodeStaticMethodEmitter : IStaticMethodEmitter
    {
        private readonly HasherResolver _resolver;

        public GetHashCodeStaticMethodEmitter(HasherResolver resolver) => _resolver = resolver;

        public void Build(Type objectType, bool detecCycles, MethodBuilder staticMethodBuilder)
        {
            using var il = staticMethodBuilder.CreateILEmitter();

            var isCollection = objectType.ImplementsGenericInterface(typeof(IEnumerable<>));
            var needNullCheck = !objectType.IsValueType && !isCollection; // collections do null check anyway

            if (needNullCheck) {
                il.LoadArgument(Arg.Input)
                  .IfTrue_S(out var next)
                  .Return(0)
                  .MarkLabel(next);
            }

            if (detecCycles) {
                EmitCycleDetection(il, objectType);
            }

            _resolver.GetHasherEmitter(new ArgumentVariable(objectType)).Emit(il);

            if (detecCycles) {
                il.Call(CycleDetectionSet.RemoveMethod, LoadArgument(Arg.CycleSet), LoadArgument(Arg.Input));
            }

            il.Return();
        }

        public bool NeedCreateCycleDetectionSets(Type _) => true;

        private static void EmitCycleDetection(ILEmitter il, Type objectType) => il
            .IfTrue_S(
                Call(CycleDetectionSet.TryAddMethod, LoadArgument(Arg.CycleSet), LoadArgument(Arg.Input), LoadInteger(0)),
                out var next)
            .Throw(New(Methods.ArgumentExceptionConstructor, LoadString($"Can't get hash for an object. Cycle is detected in {objectType.DisplayName()}.")))
            .MarkLabel(next);
    }
}