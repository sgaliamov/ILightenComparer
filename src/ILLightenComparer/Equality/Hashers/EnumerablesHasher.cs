﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Config;
using ILLightenComparer.Extensions;
using ILLightenComparer.Variables;
using Illuminator;
using Illuminator.Extensions;
using static Illuminator.Functional;

namespace ILLightenComparer.Equality.Hashers
{
    internal sealed class EnumerablesHasher : IHasherEmitter
    {
        private readonly IConfigurationProvider _configuration;
        private readonly IVariable _variable;
        private readonly HasherResolver _resolver;
        private readonly Type _enumeratorType;
        private readonly Type _elementType;
        private readonly MethodInfo _getEnumeratorMethod;
        private readonly MethodInfo _moveNextMethod;
        private readonly MethodInfo _getCurrentMethod;
        private readonly ArrayHashEmitter _arrayHashEmitter;

        private EnumerablesHasher(HasherResolver resolver, IConfigurationProvider configuration, IVariable variable)
        {
            _resolver = resolver;
            _configuration = configuration;
            _variable = variable;

            var variableType = variable.VariableType;

            _elementType = variableType
               .FindGenericInterface(typeof(IEnumerable<>))
               .GetGenericArguments()
               .Single();

            _getEnumeratorMethod = variableType.FindMethod(nameof(IEnumerable.GetEnumerator), Type.EmptyTypes);
            _enumeratorType = _getEnumeratorMethod.ReturnType;
            _moveNextMethod = _enumeratorType.FindMethod(nameof(IEnumerator.MoveNext), Type.EmptyTypes);
            _getCurrentMethod = _enumeratorType.GetPropertyGetter(nameof(IEnumerator.Current));
            _arrayHashEmitter = new ArrayHashEmitter(resolver, variable);
        }

        public static EnumerablesHasher Create(HasherResolver resolver, IConfigurationProvider configuration, IVariable variable)
        {
            var variableType = variable.VariableType;
            if (variableType.ImplementsGenericInterface(typeof(IEnumerable<>)) && !variableType.IsArray) {
                return new EnumerablesHasher(resolver, configuration, variable);
            }

            return null;
        }

        public ILEmitter Emit(ILEmitter il)
        {
            var config = _configuration.Get(_variable.OwnerType);

            return il
                .LoadLong(config.HashSeed)
                .Store(typeof(long), out var hash)
                .Execute(this.Emit(hash));
        }

        public ILEmitter Emit(ILEmitter il, LocalBuilder hash)
        {
            il.Execute(_variable.Load(Arg.Input))
              .Store(_variable.VariableType, out var enumerable)
              .DefineLabel(out var end);

            if (!_variable.VariableType.IsValueType) {
                il.IfTrue_S(LoadLocal(enumerable), out var begin)
                  .LoadInteger(0)
                  .GoTo(end)
                  .MarkLabel(begin);
            }

            if (_configuration.Get(_variable.OwnerType).IgnoreCollectionOrder) {
                return EmitHashAsSortedArray(il, enumerable, hash).MarkLabel(end);
            }

            il.Call(_getEnumeratorMethod, LoadCaller(enumerable))
              .Store(_enumeratorType, out var enumerator)
              .DefineLabel(out var loopStart);

            if (!_enumeratorType.IsValueType) {
                il.IfTrue_S(LoadLocal(enumerator), loopStart)
                  .LoadInteger(0)
                  .GoTo(end);
            }

            // todo: 1. think how to use try/finally block
            // the problem now with the inner `return` statements, it has to be `leave` instruction
            //il.BeginExceptionBlock(); 

            Loop(il, enumerator, loopStart, hash);

            //il.BeginFinallyBlock();
            EmitDisposeEnumerator(il, enumerator);

            //il.EndExceptionBlock();

            return il.LoadLocal(hash).MarkLabel(end);
        }

        private ILEmitter EmitHashAsSortedArray(ILEmitter il, LocalBuilder enumerable, LocalBuilder hash)
        {
            var hasCustomComparer = _configuration.HasCustomComparer(_elementType);

            il.EmitArraySorting(hasCustomComparer, _elementType, enumerable);

            var arrayType = _elementType.MakeArrayType();

            return _arrayHashEmitter.Emit(il, arrayType, enumerable, hash);
        }

        private void Loop(ILEmitter il, LocalBuilder enumerator, Label loopStart, LocalBuilder hash)
        {
            il.DefineLabel(out var loopEnd);

            using (il.LocalsScope()) {
                il.MarkLabel(loopStart)
                  .IfTrue_S(Call(_moveNextMethod, LoadCaller(enumerator)), out var next)
                  .GoTo(loopEnd)
                  .MarkLabel(next);
            }

            using (il.LocalsScope()) {
                var enumerators = new Dictionary<ushort, LocalBuilder>(1) { [Arg.Input] = enumerator };
                var itemVariable = new EnumerableItemVariable(_enumeratorType, _elementType, _getCurrentMethod, enumerators);

                _resolver
                    .GetHasherEmitter(itemVariable)
                    .EmitHashing(il, hash)
                    .GoTo(loopStart)
                    .MarkLabel(loopEnd);
            }
        }

        private static void EmitDisposeEnumerator(ILEmitter il, LocalBuilder enumerator) => il.EmitDispose(enumerator);
    }
}
