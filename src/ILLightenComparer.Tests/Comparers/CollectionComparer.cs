﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ILLightenComparer.Tests.Utilities;

namespace ILLightenComparer.Tests.Comparers
{
    public sealed class CollectionComparer<TItem> : IComparer<IEnumerable<TItem>>, IComparer
    {
        private readonly IComparer<TItem> _itemComparer;
        private readonly bool _sort;

        public CollectionComparer(IComparer<TItem> itemComparer = null, bool sort = false)
        {
            _sort = sort;
            _itemComparer = itemComparer ?? Helper.DefaultComparer<TItem>();
        }

        public int Compare(object x, object y) => Compare((IEnumerable<TItem>)x, (IEnumerable<TItem>)y);

        public int Compare(IEnumerable<TItem> x, IEnumerable<TItem> y)
        {
            if (x == null) {
                return y == null ? 0 : -1;
            }

            if (y == null) {
                return 1;
            }

            if (_sort) {
                var ax = x.ToArray();
                Array.Sort(ax, _itemComparer);
                x = ax;

                var ay = y.ToArray();
                Array.Sort(ay, _itemComparer);
                y = ay;
            }

            using var enumeratorX = x.GetEnumerator();
            using var enumeratorY = y.GetEnumerator();

            while (true) {
                var xDone = !enumeratorX.MoveNext();
                var yDone = !enumeratorY.MoveNext();

                if (xDone) {
                    return yDone ? 0 : -1;
                }

                if (yDone) {
                    return 1;
                }

                var xCurrent = enumeratorX.Current;
                var yCurrent = enumeratorY.Current;

                var compare = _itemComparer.Compare(xCurrent, yCurrent);
                if (compare != 0) {
                    return compare;
                }
            }
        }
    }
}
