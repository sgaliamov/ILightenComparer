﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Extensions;
using ILLightenComparer.Variables;
using Illuminator;
using static Illuminator.Functional;

namespace ILLightenComparer.Shared.Comparisons
{
    internal sealed class ArrayComparisonEmitter
    {
        private readonly IResolver _resolver;
        private readonly EmitReferenceComparisonDelegate _emitReferenceComparison;
        private readonly EmitCheckIfLoopsAreDoneDelegate _emitCheckIfLoopsAreDone;

        public ArrayComparisonEmitter(
            IResolver resolver,
            EmitCheckIfLoopsAreDoneDelegate emitCheckIfLoopsAreDone,
            EmitReferenceComparisonDelegate emitReferenceComparison)
        {
            _resolver = resolver;
            _emitCheckIfLoopsAreDone = emitCheckIfLoopsAreDone;
            _emitReferenceComparison = emitReferenceComparison;
        }

        public (LocalBuilder collectionX, LocalBuilder collectionY) EmitLoad(IVariable variable, ILEmitter il, Label gotoNext)
        {
            variable.Load(il, Arg.X).Store(variable.VariableType, out var collectionX);
            variable.Load(il, Arg.Y).Store(variable.VariableType, out var collectionY);

            if (!variable.VariableType.IsValueType) {
                _emitReferenceComparison(il, LoadLocal(collectionX), LoadLocal(collectionY), GoTo(gotoNext)); // need, because a collection can be a member of an object
            }

            return (collectionX, collectionY);
        }

        public ILEmitter EmitCompareArrays(
            ILEmitter il,
            Type arrayType,
            Type ownerType,
            LocalBuilder xArray,
            LocalBuilder yArray,
            Label afterLoop)
        {
            // todo: 2. compare array lengths at the beginning
            il.EmitArrayLength(arrayType, xArray, out var countX)
              .EmitArrayLength(arrayType, yArray, out var countY)
              .LoadInteger(0)
              .Store(typeof(int), out var index)
              .DefineLabel(out var loopStart)
              .DefineLabel(out var continueLoop)
              .MarkLabel(loopStart);

            using (il.LocalsScope()) {
                il.AreSame(LoadLocal(index), LoadLocal(countX), out var isDoneX)
                  .AreSame(LoadLocal(index), LoadLocal(countY), out var isDoneY);
                _emitCheckIfLoopsAreDone(il, isDoneX, isDoneY, afterLoop);
            }

            using (il.LocalsScope()) {
                var arrays = new Dictionary<ushort, LocalBuilder>(2) {
                    [Arg.X] = xArray,
                    [Arg.Y] = yArray
                };
                var itemVariable = new ArrayItemVariable(arrayType, ownerType, arrays, index);
                var itemComparison = _resolver.GetComparisonEmitter(itemVariable);

                return il
                    .Execute(
                        itemComparison.Emit(continueLoop),
                        itemComparison.EmitCheckForResult(continueLoop))
                    .MarkLabel(continueLoop)
                    .Add(LoadLocal(index), LoadInteger(1))
                    .Store(index)
                    .GoTo(loopStart);
            }
        }
    }
}
