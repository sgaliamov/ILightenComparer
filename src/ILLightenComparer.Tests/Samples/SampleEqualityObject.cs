﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLightenComparer.Tests.Utilities;

namespace ILLightenComparer.Tests.Samples
{
    [SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Test class")]
    public sealed class SampleEqualityObject<TMember> : IComparable<SampleEqualityObject<TMember>>
    {
        private readonly static IComparer<TMember> Comparer = Comparer<TMember>.Default;

        public TMember Field;
        public TMember Property { get; set; }

        public override string ToString() => $"Object: {this.ToJson()}";

        public int CompareTo([AllowNull] SampleEqualityObject<TMember> other)
        {
            if (ReferenceEquals(this, other)) {
                return 0;
            }

            if (other is null) {
                return 1;
            }

            var compare = Comparer.Compare(Field, other.Field);
            if (compare != 0) {
                return compare;
            }

            return Comparer.Compare(Property, other.Property);
        }
    }
}
