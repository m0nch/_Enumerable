using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Generic
{
    namespace System.Linq
    {
        public static partial class _Enumerable
        {
            public static IEnumerable<TSource> _Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                if (source is _Iterator<TSource>) return ((_Iterator<TSource>)source)._Where(predicate);
                if (source is TSource[]) return new _WhereArrayIterator<TSource>((TSource[])source, predicate);
                if (source is List<TSource>) return new _WhereListIterator<TSource>((List<TSource>)source, predicate);
                return new _WhereEnumerableIterator<TSource>(source, predicate);
            }

            public static IEnumerable<TSource> _Where<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                return _WhereIterator<TSource>(source, predicate);
            }

            static IEnumerable<TSource> _WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
            {
                int index = -1;
                foreach (TSource element in source)
                {
                    checked { index++; }
                    if (predicate(element, index)) yield return element;
                }
            }

            public static IEnumerable<TResult> _Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                if (source == null) throw new Exception("source");
                if (selector == null) throw new Exception("selector");
                if (source is _Iterator<TSource>) return ((_Iterator<TSource>)source)._Select(selector);
                if (source is TSource[]) return new _WhereSelectArrayIterator<TSource, TResult>((TSource[])source, null, selector);
                if (source is List<TSource>) return new _WhereSelectListIterator<TSource, TResult>((List<TSource>)source, null, selector);
                return new _WhereSelectEnumerableIterator<TSource, TResult>(source, null, selector);
            }

            public static IEnumerable<TResult> _Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
            {
                if (source == null) throw new Exception("source");
                if (selector == null) throw new Exception("selector");
                return _SelectIterator<TSource, TResult>(source, selector);
            }

            static IEnumerable<TResult> _SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
            {
                int index = -1;
                foreach (TSource element in source)
                {
                    checked { index++; }
                    yield return selector(element, index);
                }
            }

            static Func<TSource, bool> _CombinePredicates<TSource>(Func<TSource, bool> predicate1, Func<TSource, bool> predicate2)
            {
                return x => predicate1(x) && predicate2(x);
            }

            static Func<TSource, TResult> _CombineSelectors<TSource, TMiddle, TResult>(Func<TSource, TMiddle> selector1, Func<TMiddle, TResult> selector2)
            {
                return x => selector2(selector1(x));
            }

            abstract class _Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
            {
                int threadId;
                internal int state;
                internal TSource current;

                public _Iterator()
                {
                    threadId = Thread.CurrentThread.ManagedThreadId;
                }

                public TSource Current
                {
                    get { return current; }
                }

                public abstract _Iterator<TSource> _Clone();

                public virtual void Dispose()
                {
                    current = default(TSource);
                    state = -1;
                }

                public IEnumerator<TSource> GetEnumerator()
                {
                    if (threadId == Thread.CurrentThread.ManagedThreadId && state == 0)
                    {
                        state = 1;
                        return this;
                    }
                    _Iterator<TSource> duplicate = _Clone();
                    duplicate.state = 1;
                    return duplicate;
                }

                public abstract bool MoveNext();

                public abstract IEnumerable<TResult> _Select<TResult>(Func<TSource, TResult> selector);

                public abstract IEnumerable<TSource> _Where(Func<TSource, bool> predicate);

                object IEnumerator.Current
                {
                    get { return Current; }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                void IEnumerator.Reset()
                {
                    throw new NotImplementedException();
                }
            }

            class _WhereEnumerableIterator<TSource> : _Iterator<TSource>
            {
                IEnumerable<TSource> source;
                Func<TSource, bool> predicate;
                IEnumerator<TSource> enumerator;

                public _WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
                {
                    this.source = source;
                    this.predicate = predicate;
                }

                public override _Iterator<TSource> _Clone()
                {
                    return new _WhereEnumerableIterator<TSource>(source, predicate);
                }

                public override void Dispose()
                {
                    if (enumerator is IDisposable) ((IDisposable)enumerator).Dispose();
                    enumerator = null;
                    base.Dispose();
                }

                public override bool MoveNext()
                {
                    switch (state)
                    {
                        case 1:
                            enumerator = source.GetEnumerator();
                            state = 2;
                            goto case 2;
                        case 2:
                            while (enumerator.MoveNext())
                            {
                                TSource item = enumerator.Current;
                                if (predicate(item))
                                {
                                    current = item;
                                    return true;
                                }
                            }
                            Dispose();
                            break;
                    }
                    return false;
                }

                public override IEnumerable<TResult> _Select<TResult>(Func<TSource, TResult> selector)
                {
                    return new _WhereSelectEnumerableIterator<TSource, TResult>(source, predicate, selector);
                }

                public override IEnumerable<TSource> _Where(Func<TSource, bool> predicate)
                {
                    return new _WhereEnumerableIterator<TSource>(source, _CombinePredicates(this.predicate, predicate));
                }
            }

            class _WhereArrayIterator<TSource> : _Iterator<TSource>
            {
                TSource[] source;
                Func<TSource, bool> predicate;
                int index;

                public _WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
                {
                    this.source = source;
                    this.predicate = predicate;
                }

                public override _Iterator<TSource> _Clone()
                {
                    return new _WhereArrayIterator<TSource>(source, predicate);
                }

                public override bool MoveNext()
                {
                    if (state == 1)
                    {
                        while (index < source.Length)
                        {
                            TSource item = source[index];
                            index++;
                            if (predicate(item))
                            {
                                current = item;
                                return true;
                            }
                        }
                        Dispose();
                    }
                    return false;
                }

                public override IEnumerable<TResult> _Select<TResult>(Func<TSource, TResult> selector)
                {
                    return new _WhereSelectArrayIterator<TSource, TResult>(source, predicate, selector);
                }

                public override IEnumerable<TSource> _Where(Func<TSource, bool> predicate)
                {
                    return new _WhereArrayIterator<TSource>(source, _CombinePredicates(this.predicate, predicate));
                }
            }

            class _WhereListIterator<TSource> : _Iterator<TSource>
            {
                List<TSource> source;
                Func<TSource, bool> predicate;
                List<TSource>.Enumerator enumerator;

                public _WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
                {
                    this.source = source;
                    this.predicate = predicate;
                }

                public override _Iterator<TSource> _Clone()
                {
                    return new _WhereListIterator<TSource>(source, predicate);
                }

                public override bool MoveNext()
                {
                    switch (state)
                    {
                        case 1:
                            enumerator = source.GetEnumerator();
                            state = 2;
                            goto case 2;
                        case 2:
                            while (enumerator.MoveNext())
                            {
                                TSource item = enumerator.Current;
                                if (predicate(item))
                                {
                                    current = item;
                                    return true;
                                }
                            }
                            Dispose();
                            break;
                    }
                    return false;
                }

                public override IEnumerable<TResult> _Select<TResult>(Func<TSource, TResult> selector)
                {
                    return new _WhereSelectListIterator<TSource, TResult>(source, predicate, selector);
                }

                public override IEnumerable<TSource> _Where(Func<TSource, bool> predicate)
                {
                    return new _WhereListIterator<TSource>(source, _CombinePredicates(this.predicate, predicate));
                }
            }

            /// <summary>
            /// An iterator that maps each item of an <see cref="IEnumerable{TSource}"/>.
            /// </summary>
            /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
            /// <typeparam name="TResult">The type of the mapped items.</typeparam>
            class _SelectEnumerableIterator<TSource, TResult> : _Iterator<TResult>, _IIListProvider<TResult>
            {
                private readonly IEnumerable<TSource> _source;
                private readonly Func<TSource, TResult> _selector;
                private IEnumerator<TSource> _enumerator;

                public _SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
                {
                    _source = source;
                    _selector = selector;
                }

                public override _Iterator<TResult> _Clone()
                {
                    return new _SelectEnumerableIterator<TSource, TResult>(_source, _selector);
                }

                public override void Dispose()
                {
                    if (_enumerator != null)
                    {
                        _enumerator.Dispose();
                        _enumerator = null;
                    }

                    base.Dispose();
                }

                public override bool MoveNext()
                {
                    switch (state)
                    {
                        case 1:
                            _enumerator = _source.GetEnumerator();
                            state = 2;
                            goto case 2;
                        case 2:
                            if (_enumerator.MoveNext())
                            {
                                current = _selector(_enumerator.Current);
                                return true;
                            }

                            Dispose();
                            break;
                    }

                    return false;
                }

                public override IEnumerable<TResult2> _Select<TResult2>(Func<TResult, TResult2> selector)
                {
                    return new _SelectEnumerableIterator<TSource, TResult2>(_source, _CombineSelectors(_selector, selector));
                }

                public override IEnumerable<TResult> _Where(Func<TResult, bool> predicate)
                {
                    return new _WhereEnumerableIterator<TResult>(this, predicate);
                }

                public TResult[] _ToArray()
                {
                    var builder = new _LargeArrayBuilder<TResult>(initialize: true);

                    foreach (TSource item in _source)
                    {
                        builder._Add(_selector(item));
                    }

                    return builder._ToArray();
                }

                public List<TResult> _ToList()
                {
                    var list = new List<TResult>();

                    foreach (TSource item in _source)
                    {
                        list.Add(_selector(item));
                    }

                    return list;
                }

                public int _GetCount(bool onlyIfCheap)
                {
                    // In case someone uses Count() to force evaluation of
                    // the selector, run it provided `onlyIfCheap` is false.

                    if (onlyIfCheap)
                    {
                        return -1;
                    }

                    int count = 0;

                    foreach (TSource item in _source)
                    {
                        _selector(item);
                        checked
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }

            class _WhereSelectEnumerableIterator<TSource, TResult> : _Iterator<TResult>
            {
                IEnumerable<TSource> source;
                Func<TSource, bool> predicate;
                Func<TSource, TResult> selector;
                IEnumerator<TSource> enumerator;

                public _WhereSelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
                {
                    this.source = source;
                    this.predicate = predicate;
                    this.selector = selector;
                }

                public override _Iterator<TResult> _Clone()
                {
                    return new _WhereSelectEnumerableIterator<TSource, TResult>(source, predicate, selector);
                }

                public override void Dispose()
                {
                    if (enumerator is IDisposable) ((IDisposable)enumerator).Dispose();
                    enumerator = null;
                    base.Dispose();
                }

                public override bool MoveNext()
                {
                    switch (state)
                    {
                        case 1:
                            enumerator = source.GetEnumerator();
                            state = 2;
                            goto case 2;
                        case 2:
                            while (enumerator.MoveNext())
                            {
                                TSource item = enumerator.Current;
                                if (predicate == null || predicate(item))
                                {
                                    current = selector(item);
                                    return true;
                                }
                            }
                            Dispose();
                            break;
                    }
                    return false;
                }

                public override IEnumerable<TResult2> _Select<TResult2>(Func<TResult, TResult2> selector)
                {
                    return new _WhereSelectEnumerableIterator<TSource, TResult2>(source, predicate, _CombineSelectors(this.selector, selector));
                }

                public override IEnumerable<TResult> _Where(Func<TResult, bool> predicate)
                {
                    return new _WhereEnumerableIterator<TResult>(this, predicate);
                }
            }

            class _WhereSelectArrayIterator<TSource, TResult> : _Iterator<TResult>
            {
                TSource[] source;
                Func<TSource, bool> predicate;
                Func<TSource, TResult> selector;
                int index;

                public _WhereSelectArrayIterator(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
                {
                    this.source = source;
                    this.predicate = predicate;
                    this.selector = selector;
                }

                public override _Iterator<TResult> _Clone()
                {
                    return new _WhereSelectArrayIterator<TSource, TResult>(source, predicate, selector);
                }

                public override bool MoveNext()
                {
                    if (state == 1)
                    {
                        while (index < source.Length)
                        {
                            TSource item = source[index];
                            index++;
                            if (predicate == null || predicate(item))
                            {
                                current = selector(item);
                                return true;
                            }
                        }
                        Dispose();
                    }
                    return false;
                }

                public override IEnumerable<TResult2> _Select<TResult2>(Func<TResult, TResult2> selector)
                {
                    return new _WhereSelectArrayIterator<TSource, TResult2>(source, predicate, _CombineSelectors(this.selector, selector));
                }

                public override IEnumerable<TResult> _Where(Func<TResult, bool> predicate)
                {
                    return new _WhereEnumerableIterator<TResult>(this, predicate);
                }
            }

            class _WhereSelectListIterator<TSource, TResult> : _Iterator<TResult>
            {
                List<TSource> source;
                Func<TSource, bool> predicate;
                Func<TSource, TResult> selector;
                List<TSource>.Enumerator enumerator;

                public _WhereSelectListIterator(List<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
                {
                    this.source = source;
                    this.predicate = predicate;
                    this.selector = selector;
                }

                public override _Iterator<TResult> _Clone()
                {
                    return new _WhereSelectListIterator<TSource, TResult>(source, predicate, selector);
                }

                public override bool MoveNext()
                {
                    switch (state)
                    {
                        case 1:
                            enumerator = source.GetEnumerator();
                            state = 2;
                            goto case 2;
                        case 2:
                            while (enumerator.MoveNext())
                            {
                                TSource item = enumerator.Current;
                                if (predicate == null || predicate(item))
                                {
                                    current = selector(item);
                                    return true;
                                }
                            }
                            Dispose();
                            break;
                    }
                    return false;
                }

                public override IEnumerable<TResult2> _Select<TResult2>(Func<TResult, TResult2> selector)
                {
                    return new _WhereSelectListIterator<TSource, TResult2>(source, predicate, _CombineSelectors(this.selector, selector));
                }

                public override IEnumerable<TResult> _Where(Func<TResult, bool> predicate)
                {
                    return new _WhereEnumerableIterator<TResult>(this, predicate);
                }
            }

            //public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) {
            //    if (source == null) throw new Exception("source");
            //    if (predicate == null) throw new Exception("predicate");
            //    return WhereIterator<TSource>(source, predicate);
            //}

            //static IEnumerable<TSource> WhereIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate) {
            //    foreach (TSource element in source) {
            //        if (predicate(element)) yield return element;
            //    }
            //}

            //public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) {
            //    if (source == null) throw new Exception("source");
            //    if (selector == null) throw new Exception("selector");
            //    return SelectIterator<TSource, TResult>(source, selector);
            //}

            //static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TResult> selector) {
            //    foreach (TSource element in source) {
            //        yield return selector(element);
            //    }
            //}

            public static IEnumerable<TResult> _SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            {
                if (source == null) throw new Exception("source");
                if (selector == null) throw new Exception("selector");
                return _SelectManyIterator<TSource, TResult>(source, selector);
            }

            static IEnumerable<TResult> _SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            {
                foreach (TSource element in source)
                {
                    foreach (TResult subElement in selector(element))
                    {
                        yield return subElement;
                    }
                }
            }

            public static IEnumerable<TResult> _SelectMany<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
            {
                if (source == null) throw new Exception("source");
                if (selector == null) throw new Exception("selector");
                return _SelectManyIterator<TSource, TResult>(source, selector);
            }

            static IEnumerable<TResult> _SelectManyIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TResult>> selector)
            {
                int index = -1;
                foreach (TSource element in source)
                {
                    checked { index++; }
                    foreach (TResult subElement in selector(element, index))
                    {
                        yield return subElement;
                    }
                }
            }
            public static IEnumerable<TResult> _SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            {
                if (source == null) throw new Exception("source");
                if (collectionSelector == null) throw new Exception("collectionSelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _SelectManyIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
            }

            static IEnumerable<TResult> _SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, int, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            {
                int index = -1;
                foreach (TSource element in source)
                {
                    checked { index++; }
                    foreach (TCollection subElement in collectionSelector(element, index))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }

            public static IEnumerable<TResult> _SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            {
                if (source == null) throw new Exception("source");
                if (collectionSelector == null) throw new Exception("collectionSelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _SelectManyIterator<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
            }

            static IEnumerable<TResult> _SelectManyIterator<TSource, TCollection, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            {
                foreach (TSource element in source)
                {
                    foreach (TCollection subElement in collectionSelector(element))
                    {
                        yield return resultSelector(element, subElement);
                    }
                }
            }

            public static IEnumerable<TSource> _Take<TSource>(this IEnumerable<TSource> source, int count)
            {
                if (source == null) throw new Exception("source");
                return _TakeIterator<TSource>(source, count);
            }

            static IEnumerable<TSource> _TakeIterator<TSource>(IEnumerable<TSource> source, int count)
            {
                if (count > 0)
                {
                    foreach (TSource element in source)
                    {
                        yield return element;
                        if (--count == 0) break;
                    }
                }
            }

            public static IEnumerable<TSource> _TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                return _TakeWhileIterator<TSource>(source, predicate);
            }

            static IEnumerable<TSource> _TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                foreach (TSource element in source)
                {
                    if (!predicate(element)) break;
                    yield return element;
                }
            }

            public static IEnumerable<TSource> _TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                return _TakeWhileIterator<TSource>(source, predicate);
            }

            static IEnumerable<TSource> _TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
            {
                int index = -1;
                foreach (TSource element in source)
                {
                    checked { index++; }
                    if (!predicate(element, index)) break;
                    yield return element;
                }
            }

            public static IEnumerable<TSource> _Skip<TSource>(this IEnumerable<TSource> source, int count)
            {
                if (source == null) throw new Exception("source");
                return _SkipIterator<TSource>(source, count);
            }

            static IEnumerable<TSource> _SkipIterator<TSource>(IEnumerable<TSource> source, int count)
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    while (count > 0 && e.MoveNext()) count--;
                    if (count <= 0)
                    {
                        while (e.MoveNext()) yield return e.Current;
                    }
                }
            }

            public static IEnumerable<TSource> _SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                return _SkipWhileIterator<TSource>(source, predicate);
            }

            static IEnumerable<TSource> _SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                bool yielding = false;
                foreach (TSource element in source)
                {
                    if (!yielding && !predicate(element)) yielding = true;
                    if (yielding) yield return element;
                }
            }

            public static IEnumerable<TSource> _SkipWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                return _SkipWhileIterator<TSource>(source, predicate);
            }

            static IEnumerable<TSource> _SkipWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
            {
                int index = -1;
                bool yielding = false;
                foreach (TSource element in source)
                {
                    checked { index++; }
                    if (!yielding && !predicate(element, index)) yielding = true;
                    if (yielding) yield return element;
                }
            }

            public static IEnumerable<TResult> _Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector)
            {
                if (outer == null) throw new Exception("outer");
                if (inner == null) throw new Exception("inner");
                if (outerKeySelector == null) throw new Exception("outerKeySelector");
                if (innerKeySelector == null) throw new Exception("innerKeySelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _JoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
            }

            public static IEnumerable<TResult> _Join<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                if (outer == null) throw new Exception("outer");
                if (inner == null) throw new Exception("inner");
                if (outerKeySelector == null) throw new Exception("outerKeySelector");
                if (innerKeySelector == null) throw new Exception("innerKeySelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _JoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
            }

            static IEnumerable<TResult> _JoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                _Lookup<TKey, TInner> lookup = _Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                foreach (TOuter item in outer)
                {
                    _Lookup<TKey, TInner>.Grouping g = lookup._GetGrouping(outerKeySelector(item), false);
                    if (g != null)
                    {
                        for (int i = 0; i < g.count; i++)
                        {
                            yield return resultSelector(item, g.elements[i]);
                        }
                    }
                }
            }

            public static IEnumerable<TResult> _GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
            {
                if (outer == null) throw new Exception("outer");
                if (inner == null) throw new Exception("inner");
                if (outerKeySelector == null) throw new Exception("outerKeySelector");
                if (innerKeySelector == null) throw new Exception("innerKeySelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _GroupJoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
            }

            public static IEnumerable<TResult> _GroupJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                if (outer == null) throw new Exception("outer");
                if (inner == null) throw new Exception("inner");
                if (outerKeySelector == null) throw new Exception("outerKeySelector");
                if (innerKeySelector == null) throw new Exception("innerKeySelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _GroupJoinIterator<TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
            }

            static IEnumerable<TResult> _GroupJoinIterator<TOuter, TInner, TKey, TResult>(IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                _Lookup<TKey, TInner> lookup = _Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
                foreach (TOuter item in outer)
                {
                    yield return resultSelector(item, lookup[outerKeySelector(item)]);
                }
            }

            public static _IOrderedEnumerable<TSource> _OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                return new _OrderedEnumerable<TSource, TKey>(source, keySelector, null, false);
            }

            public static _IOrderedEnumerable<TSource> _OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
            {
                return new _OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false);
            }

            public static _IOrderedEnumerable<TSource> _OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                return new _OrderedEnumerable<TSource, TKey>(source, keySelector, null, true);
            }

            public static _IOrderedEnumerable<TSource> _OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
            {
                return new _OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true);
            }

            public static _IOrderedEnumerable<TSource> _ThenBy<TSource, TKey>(this _IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                if (source == null) throw new Exception("source");
                return source.CreateOrderedEnumerable<TKey>(keySelector, null, false);
            }

            public static _IOrderedEnumerable<TSource> _ThenBy<TSource, TKey>(this _IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
            {
                if (source == null) throw new Exception("source");
                return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, false);
            }

            public static _IOrderedEnumerable<TSource> _ThenByDescending<TSource, TKey>(this _IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                if (source == null) throw new Exception("source");
                return source.CreateOrderedEnumerable<TKey>(keySelector, null, true);
            }

            public static _IOrderedEnumerable<TSource> _ThenByDescending<TSource, TKey>(this _IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
            {
                if (source == null) throw new Exception("source");
                return source.CreateOrderedEnumerable<TKey>(keySelector, comparer, true);
            }

            public static IEnumerable<_IGrouping<TKey, TSource>> _GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                return new _GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, _IdentityFunction<TSource>.Instance, null);
            }

            public static IEnumerable<_IGrouping<TKey, TSource>> _GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
            {
                return new _GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, _IdentityFunction<TSource>.Instance, comparer);
            }

            public static IEnumerable<_IGrouping<TKey, TElement>> _GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            {
                return new _GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
            }

            public static IEnumerable<_IGrouping<TKey, TElement>> _GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
            {
                return new _GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
            }

            public static IEnumerable<TResult> _GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
            {
                return new _GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector, _IdentityFunction<TSource>.Instance, resultSelector, null);
            }

            public static IEnumerable<TResult> _GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
            {
                return new _GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);
            }

            public static IEnumerable<TResult> _GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                return new _GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector, _IdentityFunction<TSource>.Instance, resultSelector, comparer);
            }

            public static IEnumerable<TResult> _GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                return new _GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
            }

            public static IEnumerable<TSource> _Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _ConcatIterator<TSource>(first, second);
            }

            static IEnumerable<TSource> _ConcatIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                foreach (TSource element in first) yield return element;
                foreach (TSource element in second) yield return element;
            }

            public static IEnumerable<TResult> _Zip<TFirst, TSecond, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                if (resultSelector == null) throw new Exception("resultSelector");
                return _ZipIterator(first, second, resultSelector);
            }

            static IEnumerable<TResult> _ZipIterator<TFirst, TSecond, TResult>(IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            {
                using (IEnumerator<TFirst> e1 = first.GetEnumerator())
                using (IEnumerator<TSecond> e2 = second.GetEnumerator())
                    while (e1.MoveNext() && e2.MoveNext())
                        yield return resultSelector(e1.Current, e2.Current);
            }


            public static IEnumerable<TSource> _Distinct<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                return _DistinctIterator<TSource>(source, null);
            }

            public static IEnumerable<TSource> _Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
            {
                if (source == null) throw new Exception("source");
                return _DistinctIterator<TSource>(source, comparer);
            }

            static IEnumerable<TSource> _DistinctIterator<TSource>(IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
            {
                _Set<TSource> set = new _Set<TSource>(comparer);
                foreach (TSource element in source)
                    if (set._Add(element)) yield return element;
            }

            public static IEnumerable<TSource> _Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _UnionIterator<TSource>(first, second, null);
            }

            public static IEnumerable<TSource> _Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _UnionIterator<TSource>(first, second, comparer);
            }

            static IEnumerable<TSource> _UnionIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                _Set<TSource> set = new _Set<TSource>(comparer);
                foreach (TSource element in first)
                    if (set._Add(element)) yield return element;
                foreach (TSource element in second)
                    if (set._Add(element)) yield return element;
            }

            public static IEnumerable<TSource> _Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _IntersectIterator<TSource>(first, second, null);
            }

            public static IEnumerable<TSource> _Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _IntersectIterator<TSource>(first, second, comparer);
            }

            static IEnumerable<TSource> _IntersectIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                _Set<TSource> set = new _Set<TSource>(comparer);
                foreach (TSource element in second) set._Add(element);
                foreach (TSource element in first)
                    if (set._Remove(element)) yield return element;
            }

            public static IEnumerable<TSource> _Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _ExceptIterator<TSource>(first, second, null);
            }

            public static IEnumerable<TSource> _Except<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                return _ExceptIterator<TSource>(first, second, comparer);
            }

            static IEnumerable<TSource> _ExceptIterator<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                _Set<TSource> set = new _Set<TSource>(comparer);
                foreach (TSource element in second) set._Add(element);
                foreach (TSource element in first)
                    if (set._Add(element)) yield return element;
            }

            public static IEnumerable<TSource> _Reverse<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                return _ReverseIterator<TSource>(source);
            }

            static IEnumerable<TSource> _ReverseIterator<TSource>(IEnumerable<TSource> source)
            {
                _Buffer<TSource> buffer = new _Buffer<TSource>(source);
                for (int i = buffer.count - 1; i >= 0; i--) yield return buffer.items[i];
            }

            public static bool _SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                return _SequenceEqual<TSource>(first, second, null);
            }

            public static bool _SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                if (comparer == null) comparer = EqualityComparer<TSource>.Default;
                if (first == null) throw new Exception("first");
                if (second == null) throw new Exception("second");
                using (IEnumerator<TSource> e1 = first.GetEnumerator())
                using (IEnumerator<TSource> e2 = second.GetEnumerator())
                {
                    while (e1.MoveNext())
                    {
                        if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current))) return false;
                    }
                    if (e2.MoveNext()) return false;
                }
                return true;
            }

            public static IEnumerable<TSource> _AsEnumerable<TSource>(this IEnumerable<TSource> source)
            {
                return source;
            }

            public static TSource[] _ToArray<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                return new _Buffer<TSource>(source)._ToArray();
            }

            public static List<TSource> _ToList<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                return new List<TSource>(source);
            }

            public static Dictionary<TKey, TSource> _ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                return _ToDictionary<TSource, TKey, TSource>(source, keySelector, _IdentityFunction<TSource>.Instance, null);
            }

            public static Dictionary<TKey, TSource> _ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
            {
                return _ToDictionary<TSource, TKey, TSource>(source, keySelector, _IdentityFunction<TSource>.Instance, comparer);
            }

            public static Dictionary<TKey, TElement> _ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            {
                return _ToDictionary<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
            }

            public static Dictionary<TKey, TElement> _ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
            {
                if (source == null) throw new Exception("source");
                if (keySelector == null) throw new Exception("keySelector");
                if (elementSelector == null) throw new Exception("elementSelector");
                Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(comparer);
                foreach (TSource element in source) d.Add(keySelector(element), elementSelector(element));
                return d;
            }

            public static _ILookup<TKey, TSource> _ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                return _Lookup<TKey, TSource>.Create(source, keySelector, _IdentityFunction<TSource>.Instance, null);
            }

            public static _ILookup<TKey, TSource> _ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
            {
                return _Lookup<TKey, TSource>.Create(source, keySelector, _IdentityFunction<TSource>.Instance, comparer);
            }

            public static _ILookup<TKey, TElement> _ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            {
                return _Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, null);
            }

            public static _ILookup<TKey, TElement> _ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
            {
                return _Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer);
            }

            public static HashSet<TSource> _ToHashSet<TSource>(this IEnumerable<TSource> source)
            {
                return source._ToHashSet(null);
            }

            public static HashSet<TSource> _ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
            {
                if (source == null) throw new Exception("source");
                return new HashSet<TSource>(source, comparer);
            }

            public static IEnumerable<TSource> _DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
            {
                return _DefaultIfEmpty(source, default(TSource));
            }

            public static IEnumerable<TSource> _DefaultIfEmpty<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
            {
                if (source == null) throw new Exception("source");
                return _DefaultIfEmptyIterator<TSource>(source, defaultValue);
            }

            static IEnumerable<TSource> _DefaultIfEmptyIterator<TSource>(IEnumerable<TSource> source, TSource defaultValue)
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        do
                        {
                            yield return e.Current;
                        } while (e.MoveNext());
                    }
                    else
                    {
                        yield return defaultValue;
                    }
                }
            }

            public static IEnumerable<TResult> _OfType<TResult>(this IEnumerable source)
            {
                if (source == null) throw new Exception("source");
                return _OfTypeIterator<TResult>(source);
            }

            static IEnumerable<TResult> _OfTypeIterator<TResult>(IEnumerable source)
            {
                foreach (object obj in source)
                {
                    if (obj is TResult) yield return (TResult)obj;
                }
            }

            public static IEnumerable<TResult> _Cast<TResult>(this IEnumerable source)
            {
                IEnumerable<TResult> typedSource = source as IEnumerable<TResult>;
                if (typedSource != null) return typedSource;
                if (source == null) throw new Exception("source");
                return _CastIterator<TResult>(source);
            }

            static IEnumerable<TResult> _CastIterator<TResult>(IEnumerable source)
            {
                foreach (object obj in source) yield return (TResult)obj;
            }

            public static TSource _First<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    if (list.Count > 0) return list[0];
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (e.MoveNext()) return e.Current;
                    }
                }
                throw new Exception();
            }

            public static TSource _First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                foreach (TSource element in source)
                {
                    if (predicate(element)) return element;
                }
                throw new Exception();
            }

            public static TSource _FirstOrDefault<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    if (list.Count > 0) return list[0];
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (e.MoveNext()) return e.Current;
                    }
                }
                return default(TSource);
            }

            public static TSource _FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                foreach (TSource element in source)
                {
                    if (predicate(element)) return element;
                }
                return default(TSource);
            }

            public static TSource _Last<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    int count = list.Count;
                    if (count > 0) return list[count - 1];
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (e.MoveNext())
                        {
                            TSource result;
                            do
                            {
                                result = e.Current;
                            } while (e.MoveNext());
                            return result;
                        }
                    }
                }
                throw new Exception();
            }

            public static TSource _Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                TSource result = default(TSource);
                bool found = false;
                foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        result = element;
                        found = true;
                    }
                }
                if (found) return result;
                throw new Exception();
            }

            public static TSource _LastOrDefault<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    int count = list.Count;
                    if (count > 0) return list[count - 1];
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (e.MoveNext())
                        {
                            TSource result;
                            do
                            {
                                result = e.Current;
                            } while (e.MoveNext());
                            return result;
                        }
                    }
                }
                return default(TSource);
            }

            public static TSource _LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                TSource result = default(TSource);
                foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        result = element;
                    }
                }
                return result;
            }

            public static TSource _Single<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    switch (list.Count)
                    {
                        case 0: throw new Exception();
                        case 1: return list[0];
                    }
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (!e.MoveNext()) throw new Exception();
                        TSource result = e.Current;
                        if (!e.MoveNext()) return result;
                    }
                }
                throw new Exception();
            }

            public static TSource _Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                TSource result = default(TSource);
                long count = 0;
                foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        result = element;
                        checked { count++; }
                    }
                }
                switch (count)
                {
                    case 0: throw new Exception();
                    case 1: return result;
                }
                throw new Exception();
            }

            public static TSource _SingleOrDefault<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    switch (list.Count)
                    {
                        case 0: return default(TSource);
                        case 1: return list[0];
                    }
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (!e.MoveNext()) return default(TSource);
                        TSource result = e.Current;
                        if (!e.MoveNext()) return result;
                    }
                }
                throw new Exception();
            }

            public static TSource _SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                TSource result = default(TSource);
                long count = 0;
                foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        result = element;
                        checked { count++; }
                    }
                }
                switch (count)
                {
                    case 0: return default(TSource);
                    case 1: return result;
                }
                throw new Exception();
            }

            public static TSource _ElementAt<TSource>(this IEnumerable<TSource> source, int index)
            {
                if (source == null) throw new Exception("source");
                IList<TSource> list = source as IList<TSource>;
                if (list != null) return list[index];
                if (index < 0) throw new ArgumentOutOfRangeException("index");
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    while (true)
                    {
                        if (!e.MoveNext()) throw new ArgumentOutOfRangeException("index");
                        if (index == 0) return e.Current;
                        index--;
                    }
                }
            }

            public static TSource _ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
            {
                if (source == null) throw new Exception("source");
                if (index >= 0)
                {
                    IList<TSource> list = source as IList<TSource>;
                    if (list != null)
                    {
                        if (index < list.Count) return list[index];
                    }
                    else
                    {
                        using (IEnumerator<TSource> e = source.GetEnumerator())
                        {
                            while (true)
                            {
                                if (!e.MoveNext()) break;
                                if (index == 0) return e.Current;
                                index--;
                            }
                        }
                    }
                }
                return default(TSource);
            }

            public static IEnumerable<int> _Range(int start, int count)
            {
                long max = ((long)start) + count - 1;
                if (count < 0 || max > Int32.MaxValue) throw new ArgumentOutOfRangeException("count");
                return _RangeIterator(start, count);
            }

            static IEnumerable<int> _RangeIterator(int start, int count)
            {
                for (int i = 0; i < count; i++) yield return start + i;
            }

            public static IEnumerable<TResult> _Repeat<TResult>(TResult element, int count)
            {
                if (count < 0) throw new ArgumentOutOfRangeException("count");
                return _RepeatIterator<TResult>(element, count);
            }

            static IEnumerable<TResult> _RepeatIterator<TResult>(TResult element, int count)
            {
                for (int i = 0; i < count; i++) yield return element;
            }

            public static IEnumerable<TResult> _Empty<TResult>()
            {
                return _EmptyEnumerable<TResult>.Instance;
            }

            public static bool _Any<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext()) return true;
                }
                return false;
            }

            public static bool _Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                foreach (TSource element in source)
                {
                    if (predicate(element)) return true;
                }
                return false;
            }

            public static bool _All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                foreach (TSource element in source)
                {
                    if (!predicate(element)) return false;
                }
                return true;
            }

            public static int _Count<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                ICollection<TSource> collectionoft = source as ICollection<TSource>;
                if (collectionoft != null) return collectionoft.Count;
                ICollection collection = source as ICollection;
                if (collection != null) return collection.Count;
                int count = 0;
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    checked
                    {
                        while (e.MoveNext()) count++;
                    }
                }
                return count;
            }

            public static int _Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                int count = 0;
                foreach (TSource element in source)
                {
                    checked
                    {
                        if (predicate(element)) count++;
                    }
                }
                return count;
            }

            public static long _LongCount<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                long count = 0;
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    checked
                    {
                        while (e.MoveNext()) count++;
                    }
                }
                return count;
            }

            public static long _LongCount<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            {
                if (source == null) throw new Exception("source");
                if (predicate == null) throw new Exception("predicate");
                long count = 0;
                foreach (TSource element in source)
                {
                    checked
                    {
                        if (predicate(element)) count++;
                    }
                }
                return count;
            }

            public static bool _Contains<TSource>(this IEnumerable<TSource> source, TSource value)
            {
                ICollection<TSource> collection = source as ICollection<TSource>;
                if (collection != null) return collection.Contains(value);
                return _Contains<TSource>(source, value, null);
            }

            public static bool _Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
            {
                if (comparer == null) comparer = EqualityComparer<TSource>.Default;
                if (source == null) throw new Exception("source");
                foreach (TSource element in source)
                    if (comparer.Equals(element, value)) return true;
                return false;
            }

            public static TSource _Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
            {
                if (source == null) throw new Exception("source");
                if (func == null) throw new Exception("func");
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext()) throw new Exception();
                    TSource result = e.Current;
                    while (e.MoveNext()) result = func(result, e.Current);
                    return result;
                }
            }

            public static TAccumulate _Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
            {
                if (source == null) throw new Exception("source");
                if (func == null) throw new Exception("func");
                TAccumulate result = seed;
                foreach (TSource element in source) result = func(result, element);
                return result;
            }

            public static TResult _Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
            {
                if (source == null) throw new Exception("source");
                if (func == null) throw new Exception("func");
                if (resultSelector == null) throw new Exception("resultSelector");
                TAccumulate result = seed;
                foreach (TSource element in source) result = func(result, element);
                return resultSelector(result);
            }

            public static int _Sum(this IEnumerable<int> source)
            {
                if (source == null) throw new Exception("source");
                int sum = 0;
                checked
                {
                    foreach (int v in source) sum += v;
                }
                return sum;
            }

            public static int? _Sum(this IEnumerable<int?> source)
            {
                if (source == null) throw new Exception("source");
                int sum = 0;
                checked
                {
                    foreach (int? v in source)
                    {
                        if (v != null) sum += v.GetValueOrDefault();
                    }
                }
                return sum;
            }

            public static long _Sum(this IEnumerable<long> source)
            {
                if (source == null) throw new Exception("source");
                long sum = 0;
                checked
                {
                    foreach (long v in source) sum += v;
                }
                return sum;
            }

            public static long? _Sum(this IEnumerable<long?> source)
            {
                if (source == null) throw new Exception("source");
                long sum = 0;
                checked
                {
                    foreach (long? v in source)
                    {
                        if (v != null) sum += v.GetValueOrDefault();
                    }
                }
                return sum;
            }

            public static float _Sum(this IEnumerable<float> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                foreach (float v in source) sum += v;
                return (float)sum;
            }

            public static float? _Sum(this IEnumerable<float?> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                foreach (float? v in source)
                {
                    if (v != null) sum += v.GetValueOrDefault();
                }
                return (float)sum;
            }

            public static double _Sum(this IEnumerable<double> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                foreach (double v in source) sum += v;
                return sum;
            }

            public static double? _Sum(this IEnumerable<double?> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                foreach (double? v in source)
                {
                    if (v != null) sum += v.GetValueOrDefault();
                }
                return sum;
            }

            public static decimal _Sum(this IEnumerable<decimal> source)
            {
                if (source == null) throw new Exception("source");
                decimal sum = 0;
                foreach (decimal v in source) sum += v;
                return sum;
            }

            public static decimal? _Sum(this IEnumerable<decimal?> source)
            {
                if (source == null) throw new Exception("source");
                decimal sum = 0;
                foreach (decimal? v in source)
                {
                    if (v != null) sum += v.GetValueOrDefault();
                }
                return sum;
            }

            public static int _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static int? _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static long _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static long? _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static float _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static float? _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static double _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static double? _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static decimal _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static decimal? _Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
            {
                return _Enumerable._Sum(_Enumerable._Select(source, selector));
            }

            public static int _Min(this IEnumerable<int> source)
            {
                if (source == null) throw new Exception("source");
                int value = 0;
                bool hasValue = false;
                foreach (int x in source)
                {
                    if (hasValue)
                    {
                        if (x < value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static int? _Min(this IEnumerable<int?> source)
            {
                if (source == null) throw new Exception("source");
                int? value = null;
                foreach (int? x in source)
                {
                    if (value == null || x < value)
                        value = x;
                }
                return value;
            }

            public static long _Min(this IEnumerable<long> source)
            {
                if (source == null) throw new Exception("source");
                long value = 0;
                bool hasValue = false;
                foreach (long x in source)
                {
                    if (hasValue)
                    {
                        if (x < value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static long? _Min(this IEnumerable<long?> source)
            {
                if (source == null) throw new Exception("source");
                long? value = null;
                foreach (long? x in source)
                {
                    if (value == null || x < value) value = x;
                }
                return value;
            }

            public static float _Min(this IEnumerable<float> source)
            {
                if (source == null) throw new Exception("source");
                float value = 0;
                bool hasValue = false;
                foreach (float x in source)
                {
                    if (hasValue)
                    {
                        // Normally NaN < anything is false, as is anything < NaN
                        // However, this leads to some irksome outcomes in Min and Max.
                        // If we use those semantics then Min(NaN, 5.0) is NaN, but
                        // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                        // ordering where NaN is smaller than every value, including
                        // negative infinity.
                        if (x < value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static float? _Min(this IEnumerable<float?> source)
            {
                if (source == null) throw new Exception("source");
                float? value = null;
                foreach (float? x in source)
                {
                    if (x == null) continue;
                    if (value == null || x < value) value = x;
                }
                return value;
            }

            public static double _Min(this IEnumerable<double> source)
            {
                if (source == null) throw new Exception("source");
                double value = 0;
                bool hasValue = false;
                foreach (double x in source)
                {
                    if (hasValue)
                    {
                        if (x < value || Double.IsNaN(x)) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static double? _Min(this IEnumerable<double?> source)
            {
                if (source == null) throw new Exception("source");
                double? value = null;
                foreach (double? x in source)
                {
                    if (x == null) continue;
                    if (value == null || x < value || Double.IsNaN((double)x)) value = x;
                }
                return value;
            }

            public static decimal _Min(this IEnumerable<decimal> source)
            {
                if (source == null) throw new Exception("source");
                decimal value = 0;
                bool hasValue = false;
                foreach (decimal x in source)
                {
                    if (hasValue)
                    {
                        if (x < value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static decimal? _Min(this IEnumerable<decimal?> source)
            {
                if (source == null) throw new Exception("source");
                decimal? value = null;
                foreach (decimal? x in source)
                {
                    if (value == null || x < value) value = x;
                }
                return value;
            }

            public static TSource _Min<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                Comparer<TSource> comparer = Comparer<TSource>.Default;
                TSource value = default(TSource);
                if (value == null)
                {
                    foreach (TSource x in source)
                    {
                        if (x != null && (value == null || comparer.Compare(x, value) < 0))
                            value = x;
                    }
                    return value;
                }
                else
                {
                    bool hasValue = false;
                    foreach (TSource x in source)
                    {
                        if (hasValue)
                        {
                            if (comparer.Compare(x, value) < 0)
                                value = x;
                        }
                        else
                        {
                            value = x;
                            hasValue = true;
                        }
                    }
                    if (hasValue) return value;
                    throw new Exception();
                }
            }

            public static int _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static int? _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static long _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static long? _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static float _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static float? _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static double _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static double? _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static decimal _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static decimal? _Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static TResult _Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                return _Enumerable._Min(_Enumerable._Select(source, selector));
            }

            public static int _Max(this IEnumerable<int> source)
            {
                if (source == null) throw new Exception("source");
                int value = 0;
                bool hasValue = false;
                foreach (int x in source)
                {
                    if (hasValue)
                    {
                        if (x > value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static int? _Max(this IEnumerable<int?> source)
            {
                if (source == null) throw new Exception("source");
                int? value = null;
                foreach (int? x in source)
                {
                    if (value == null || x > value) value = x;
                }
                return value;
            }

            public static long _Max(this IEnumerable<long> source)
            {
                if (source == null) throw new Exception("source");
                long value = 0;
                bool hasValue = false;
                foreach (long x in source)
                {
                    if (hasValue)
                    {
                        if (x > value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static long? _Max(this IEnumerable<long?> source)
            {
                if (source == null) throw new Exception("source");
                long? value = null;
                foreach (long? x in source)
                {
                    if (value == null || x > value) value = x;
                }
                return value;
            }

            public static double _Max(this IEnumerable<double> source)
            {
                if (source == null) throw new Exception("source");
                double value = 0;
                bool hasValue = false;
                foreach (double x in source)
                {
                    if (hasValue)
                    {
                        if (x > value || Double.IsNaN(value)) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static double? _Max(this IEnumerable<double?> source)
            {
                if (source == null) throw new Exception("source");
                double? value = null;
                foreach (double? x in source)
                {
                    if (x == null) continue;
                    if (value == null || x > value || Double.IsNaN((double)value)) value = x;
                }
                return value;
            }

            public static float _Max(this IEnumerable<float> source)
            {
                if (source == null) throw new Exception("source");
                float value = 0;
                bool hasValue = false;
                foreach (float x in source)
                {
                    if (hasValue)
                    {
                        if (x > value || Double.IsNaN(value)) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static float? _Max(this IEnumerable<float?> source)
            {
                if (source == null) throw new Exception("source");
                float? value = null;
                foreach (float? x in source)
                {
                    if (x == null) continue;
                    if (value == null || x > value) value = x;
                }
                return value;
            }

            public static decimal _Max(this IEnumerable<decimal> source)
            {
                if (source == null) throw new Exception("source");
                decimal value = 0;
                bool hasValue = false;
                foreach (decimal x in source)
                {
                    if (hasValue)
                    {
                        if (x > value) value = x;
                    }
                    else
                    {
                        value = x;
                        hasValue = true;
                    }
                }
                if (hasValue) return value;
                throw new Exception();
            }

            public static decimal? _Max(this IEnumerable<decimal?> source)
            {
                if (source == null) throw new Exception("source");
                decimal? value = null;
                foreach (decimal? x in source)
                {
                    if (value == null || x > value) value = x;
                }
                return value;
            }

            public static TSource _Max<TSource>(this IEnumerable<TSource> source)
            {
                if (source == null) throw new Exception("source");
                Comparer<TSource> comparer = Comparer<TSource>.Default;
                TSource value = default(TSource);
                if (value == null)
                {
                    foreach (TSource x in source)
                    {
                        if (x != null && (value == null || comparer.Compare(x, value) > 0))
                            value = x;
                    }
                    return value;
                }
                else
                {
                    bool hasValue = false;
                    foreach (TSource x in source)
                    {
                        if (hasValue)
                        {
                            if (comparer.Compare(x, value) > 0)
                                value = x;
                        }
                        else
                        {
                            value = x;
                            hasValue = true;
                        }
                    }
                    if (hasValue) return value;
                    throw new Exception();
                }
            }

            public static int _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static int? _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static long _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static long? _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static float _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static float? _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static double _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static double? _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static decimal _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static decimal? _Max<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static TResult _Max<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                return _Enumerable._Max(_Enumerable._Select(source, selector));
            }

            public static double _Average(this IEnumerable<int> source)
            {
                if (source == null) throw new Exception("source");
                long sum = 0;
                long count = 0;
                checked
                {
                    foreach (int v in source)
                    {
                        sum += v;
                        count++;
                    }
                }
                if (count > 0) return (double)sum / count;
                throw new Exception();
            }

            public static double? _Average(this IEnumerable<int?> source)
            {
                if (source == null) throw new Exception("source");
                long sum = 0;
                long count = 0;
                checked
                {
                    foreach (int? v in source)
                    {
                        if (v != null)
                        {
                            sum += v.GetValueOrDefault();
                            count++;
                        }
                    }
                }
                if (count > 0) return (double)sum / count;
                return null;
            }

            public static double _Average(this IEnumerable<long> source)
            {
                if (source == null) throw new Exception("source");
                long sum = 0;
                long count = 0;
                checked
                {
                    foreach (long v in source)
                    {
                        sum += v;
                        count++;
                    }
                }
                if (count > 0) return (double)sum / count;
                throw new Exception();
            }

            public static double? _Average(this IEnumerable<long?> source)
            {
                if (source == null) throw new Exception("source");
                long sum = 0;
                long count = 0;
                checked
                {
                    foreach (long? v in source)
                    {
                        if (v != null)
                        {
                            sum += v.GetValueOrDefault();
                            count++;
                        }
                    }
                }
                if (count > 0) return (double)sum / count;
                return null;
            }

            public static float _Average(this IEnumerable<float> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                long count = 0;
                checked
                {
                    foreach (float v in source)
                    {
                        sum += v;
                        count++;
                    }
                }
                if (count > 0) return (float)(sum / count);
                throw new Exception();
            }

            public static float? _Average(this IEnumerable<float?> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                long count = 0;
                checked
                {
                    foreach (float? v in source)
                    {
                        if (v != null)
                        {
                            sum += v.GetValueOrDefault();
                            count++;
                        }
                    }
                }
                if (count > 0) return (float)(sum / count);
                return null;
            }

            public static double _Average(this IEnumerable<double> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                long count = 0;
                checked
                {
                    foreach (double v in source)
                    {
                        sum += v;
                        count++;
                    }
                }
                if (count > 0) return sum / count;
                throw new Exception();
            }

            public static double? _Average(this IEnumerable<double?> source)
            {
                if (source == null) throw new Exception("source");
                double sum = 0;
                long count = 0;
                checked
                {
                    foreach (double? v in source)
                    {
                        if (v != null)
                        {
                            sum += v.GetValueOrDefault();
                            count++;
                        }
                    }
                }
                if (count > 0) return sum / count;
                return null;
            }

            public static decimal _Average(this IEnumerable<decimal> source)
            {
                if (source == null) throw new Exception("source");
                decimal sum = 0;
                long count = 0;
                checked
                {
                    foreach (decimal v in source)
                    {
                        sum += v;
                        count++;
                    }
                }
                if (count > 0) return sum / count;
                throw new Exception();
            }

            public static decimal? _Average(this IEnumerable<decimal?> source)
            {
                if (source == null) throw new Exception("source");
                decimal sum = 0;
                long count = 0;
                checked
                {
                    foreach (decimal? v in source)
                    {
                        if (v != null)
                        {
                            sum += v.GetValueOrDefault();
                            count++;
                        }
                    }
                }
                if (count > 0) return sum / count;
                return null;
            }

            public static double _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static double? _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static double _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static double? _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static float _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static float? _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static double _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static double? _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static decimal _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }

            public static decimal? _Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
            {
                return _Enumerable._Average(_Enumerable._Select(source, selector));
            }
        }


        //
        // We have added some optimization in SZArrayHelper class to cache the enumerator of zero length arrays so  
        // the enumerator will be created once per type.
        // 
        internal class _EmptyEnumerable<TElement>
        {
            public static readonly TElement[] Instance = new TElement[0];
        }

        internal class _IdentityFunction<TElement>
        {
            public static Func<TElement, TElement> Instance
            {
                get { return x => x; }
            }
        }

        public interface _IOrderedEnumerable<TElement> : IEnumerable<TElement>
        {
            _IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending);
        }

#if SILVERLIGHT && !FEATURE_NETCORE
    public interface IGrouping<TKey, TElement> : IEnumerable<TElement>
#else
        public interface _IGrouping<out TKey, out TElement> : IEnumerable<TElement>
#endif
        {
            TKey Key { get; }
        }

        public interface _ILookup<TKey, TElement> : IEnumerable<_IGrouping<TKey, TElement>>
        {
            int Count { get; }
            IEnumerable<TElement> this[TKey key] { get; }
            bool Contains(TKey key);
        }

        public class _Lookup<TKey, TElement> : IEnumerable<_IGrouping<TKey, TElement>>, _ILookup<TKey, TElement>
        {
            IEqualityComparer<TKey> comparer;
            Grouping[] groupings;
            Grouping lastGrouping;
            int count;

            internal static _Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
            {
                if (source == null) throw new Exception("source");
                if (keySelector == null) throw new Exception("keySelector");
                if (elementSelector == null) throw new Exception("elementSelector");
                _Lookup<TKey, TElement> lookup = new _Lookup<TKey, TElement>(comparer);
                foreach (TSource item in source)
                {
                    lookup._GetGrouping(keySelector(item), true)._Add(elementSelector(item));
                }
                return lookup;
            }

            internal static _Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
            {
                _Lookup<TKey, TElement> lookup = new _Lookup<TKey, TElement>(comparer);
                foreach (TElement item in source)
                {
                    TKey key = keySelector(item);
                    if (key != null) lookup._GetGrouping(key, true)._Add(item);
                }
                return lookup;
            }

            _Lookup(IEqualityComparer<TKey> comparer)
            {
                if (comparer == null) comparer = EqualityComparer<TKey>.Default;
                this.comparer = comparer;
                groupings = new Grouping[7];
            }

            public int Count
            {
                get { return count; }
            }

            public IEnumerable<TElement> this[TKey key]
            {
                get
                {
                    Grouping grouping = _GetGrouping(key, false);
                    if (grouping != null) return grouping;
                    return _EmptyEnumerable<TElement>.Instance;
                }
            }

            public bool Contains(TKey key)
            {
                return _GetGrouping(key, false) != null;
            }

            public IEnumerator<_IGrouping<TKey, TElement>> GetEnumerator()
            {
                Grouping g = lastGrouping;
                if (g != null)
                {
                    do
                    {
                        g = g.next;
                        yield return g;
                    } while (g != lastGrouping);
                }
            }

            public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
            {
                Grouping g = lastGrouping;
                if (g != null)
                {
                    do
                    {
                        g = g.next;
                        if (g.count != g.elements.Length) { Array.Resize<TElement>(ref g.elements, g.count); }
                        yield return resultSelector(g.key, g.elements);
                    } while (g != lastGrouping);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            internal int _InternalGetHashCode(TKey key)
            {
                //Microsoft DevDivBugs 171937. work around comparer implementations that throw when passed null
                return (key == null) ? 0 : comparer.GetHashCode(key) & 0x7FFFFFFF;
            }

            internal Grouping _GetGrouping(TKey key, bool create)
            {
                int hashCode = _InternalGetHashCode(key);
                for (Grouping g = groupings[hashCode % groupings.Length]; g != null; g = g.hashNext)
                    if (g.hashCode == hashCode && comparer.Equals(g.key, key)) return g;
                if (create)
                {
                    if (count == groupings.Length) _Resize();
                    int index = hashCode % groupings.Length;
                    Grouping g = new Grouping();
                    g.key = key;
                    g.hashCode = hashCode;
                    g.elements = new TElement[1];
                    g.hashNext = groupings[index];
                    groupings[index] = g;
                    if (lastGrouping == null)
                    {
                        g.next = g;
                    }
                    else
                    {
                        g.next = lastGrouping.next;
                        lastGrouping.next = g;
                    }
                    lastGrouping = g;
                    count++;
                    return g;
                }
                return null;
            }

            void _Resize()
            {
                int newSize = checked(count * 2 + 1);
                Grouping[] newGroupings = new Grouping[newSize];
                Grouping g = lastGrouping;
                do
                {
                    g = g.next;
                    int index = g.hashCode % newSize;
                    g.hashNext = newGroupings[index];
                    newGroupings[index] = g;
                } while (g != lastGrouping);
                groupings = newGroupings;
            }

            internal class Grouping : _IGrouping<TKey, TElement>, IList<TElement>
            {
                internal TKey key;
                internal int hashCode;
                internal TElement[] elements;
                internal int count;
                internal Grouping hashNext;
                internal Grouping next;

                internal void _Add(TElement element)
                {
                    if (elements.Length == count) Array.Resize(ref elements, checked(count * 2));
                    elements[count] = element;
                    count++;
                }

                public IEnumerator<TElement> GetEnumerator()
                {
                    for (int i = 0; i < count; i++) yield return elements[i];
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return GetEnumerator();
                }

                // DDB195907: implement IGrouping<>.Key implicitly
                // so that WPF binding works on this property.
                public TKey Key
                {
                    get { return key; }
                }

                int ICollection<TElement>.Count
                {
                    get { return count; }
                }

                bool ICollection<TElement>.IsReadOnly
                {
                    get { return true; }
                }

                void ICollection<TElement>.Add(TElement item)
                {
                    throw new Exception();
                }

                void ICollection<TElement>.Clear()
                {
                    throw new Exception();
                }

                bool ICollection<TElement>.Contains(TElement item)
                {
                    return Array.IndexOf(elements, item, 0, count) >= 0;
                }

                void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
                {
                    Array.Copy(elements, 0, array, arrayIndex, count);
                }

                bool ICollection<TElement>.Remove(TElement item)
                {
                    throw new Exception();
                }

                int IList<TElement>.IndexOf(TElement item)
                {
                    return Array.IndexOf(elements, item, 0, count);
                }

                void IList<TElement>.Insert(int index, TElement item)
                {
                    throw new Exception();
                }

                void IList<TElement>.RemoveAt(int index)
                {
                    throw new Exception();
                }

                TElement IList<TElement>.this[int index]
                {
                    get
                    {
                        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException("index");
                        return elements[index];
                    }
                    set
                    {
                        throw new Exception();
                    }
                }
            }
        }

        // @
        internal class _Set<TElement>
        {
            int[] buckets;
            Slot[] slots;
            int count;
            int freeList;
            IEqualityComparer<TElement> comparer;

            public _Set() : this(null) { }

            public _Set(IEqualityComparer<TElement> comparer)
            {
                if (comparer == null) comparer = EqualityComparer<TElement>.Default;
                this.comparer = comparer;
                buckets = new int[7];
                slots = new Slot[7];
                freeList = -1;
            }

            // If value is not in set, add it and return true; otherwise return false
            public bool _Add(TElement value)
            {
                return !_Find(value, true);
            }

            // Check whether value is in set
            public bool _Contains(TElement value)
            {
                return _Find(value, false);
            }

            // If value is in set, remove it and return true; otherwise return false
            public bool _Remove(TElement value)
            {
                int hashCode = _InternalGetHashCode(value);
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket] - 1; i >= 0; last = i, i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value))
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = slots[i].next + 1;
                        }
                        else
                        {
                            slots[last].next = slots[i].next;
                        }
                        slots[i].hashCode = -1;
                        slots[i].value = default(TElement);
                        slots[i].next = freeList;
                        freeList = i;
                        return true;
                    }
                }
                return false;
            }

            bool _Find(TElement value, bool add)
            {
                int hashCode = _InternalGetHashCode(value);
                for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
                {
                    if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value)) return true;
                }
                if (add)
                {
                    int index;
                    if (freeList >= 0)
                    {
                        index = freeList;
                        freeList = slots[index].next;
                    }
                    else
                    {
                        if (count == slots.Length) _Resize();
                        index = count;
                        count++;
                    }
                    int bucket = hashCode % buckets.Length;
                    slots[index].hashCode = hashCode;
                    slots[index].value = value;
                    slots[index].next = buckets[bucket] - 1;
                    buckets[bucket] = index + 1;
                }
                return false;
            }

            void _Resize()
            {
                int newSize = checked(count * 2 + 1);
                int[] newBuckets = new int[newSize];
                Slot[] newSlots = new Slot[newSize];
                Array.Copy(slots, 0, newSlots, 0, count);
                for (int i = 0; i < count; i++)
                {
                    int bucket = newSlots[i].hashCode % newSize;
                    newSlots[i].next = newBuckets[bucket] - 1;
                    newBuckets[bucket] = i + 1;
                }
                buckets = newBuckets;
                slots = newSlots;
            }

            internal int _InternalGetHashCode(TElement value)
            {
                //Microsoft DevDivBugs 171937. work around comparer implementations that throw when passed null
                return (value == null) ? 0 : comparer.GetHashCode(value) & 0x7FFFFFFF;
            }

            internal struct Slot
            {
                internal int hashCode;
                internal TElement value;
                internal int next;
            }
        }

        internal class _GroupedEnumerable<TSource, TKey, TElement, TResult> : IEnumerable<TResult>
        {
            IEnumerable<TSource> source;
            Func<TSource, TKey> keySelector;
            Func<TSource, TElement> elementSelector;
            IEqualityComparer<TKey> comparer;
            Func<TKey, IEnumerable<TElement>, TResult> resultSelector;

            public _GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
            {
                if (source == null) throw new Exception("source");
                if (keySelector == null) throw new Exception("keySelector");
                if (elementSelector == null) throw new Exception("elementSelector");
                if (resultSelector == null) throw new Exception("resultSelector");
                this.source = source;
                this.keySelector = keySelector;
                this.elementSelector = elementSelector;
                this.comparer = comparer;
                this.resultSelector = resultSelector;
            }

            public IEnumerator<TResult> GetEnumerator()
            {
                _Lookup<TKey, TElement> lookup = _Lookup<TKey, TElement>.Create<TSource>(source, keySelector, elementSelector, comparer);
                return lookup.ApplyResultSelector(resultSelector).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class _GroupedEnumerable<TSource, TKey, TElement> : IEnumerable<_IGrouping<TKey, TElement>>
        {
            IEnumerable<TSource> source;
            Func<TSource, TKey> keySelector;
            Func<TSource, TElement> elementSelector;
            IEqualityComparer<TKey> comparer;

            public _GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
            {
                if (source == null) throw new Exception("source");
                if (keySelector == null) throw new Exception("keySelector");
                if (elementSelector == null) throw new Exception("elementSelector");
                this.source = source;
                this.keySelector = keySelector;
                this.elementSelector = elementSelector;
                this.comparer = comparer;
            }

            public IEnumerator<_IGrouping<TKey, TElement>> GetEnumerator()
            {
                return _Lookup<TKey, TElement>.Create<TSource>(source, keySelector, elementSelector, comparer).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal abstract class _OrderedEnumerable<TElement> : _IOrderedEnumerable<TElement>
        {
            internal IEnumerable<TElement> source;

            public IEnumerator<TElement> GetEnumerator()
            {
                _Buffer<TElement> buffer = new _Buffer<TElement>(source);
                if (buffer.count > 0)
                {
                    _EnumerableSorter<TElement> sorter = GetEnumerableSorter(null);
                    int[] map = sorter.Sort(buffer.items, buffer.count);
                    sorter = null;
                    for (int i = 0; i < buffer.count; i++) yield return buffer.items[map[i]];
                }
            }

            internal abstract _EnumerableSorter<TElement> GetEnumerableSorter(_EnumerableSorter<TElement> next);

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            _IOrderedEnumerable<TElement> _IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            {
                _OrderedEnumerable<TElement, TKey> result = new _OrderedEnumerable<TElement, TKey>(source, keySelector, comparer, descending);
                result.parent = this;
                return result;
            }
        }

        internal class _OrderedEnumerable<TElement, TKey> : _OrderedEnumerable<TElement>
        {
            internal _OrderedEnumerable<TElement> parent;
            internal Func<TElement, TKey> keySelector;
            internal IComparer<TKey> comparer;
            internal bool descending;

            internal _OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            {
                if (source == null) throw new Exception("source");
                if (keySelector == null) throw new Exception("keySelector");
                this.source = source;
                this.parent = null;
                this.keySelector = keySelector;
                this.comparer = comparer != null ? comparer : Comparer<TKey>.Default;
                this.descending = descending;
            }

            internal override _EnumerableSorter<TElement> GetEnumerableSorter(_EnumerableSorter<TElement> next)
            {
                _EnumerableSorter<TElement> sorter = new _EnumerableSorter<TElement, TKey>(keySelector, comparer, descending, next);
                if (parent != null) sorter = parent.GetEnumerableSorter(sorter);
                return sorter;
            }
        }

        internal abstract class _EnumerableSorter<TElement>
        {
            internal abstract void _ComputeKeys(TElement[] elements, int count);

            internal abstract int _CompareKeys(int index1, int index2);

            internal int[] Sort(TElement[] elements, int count)
            {
                _ComputeKeys(elements, count);
                int[] map = new int[count];
                for (int i = 0; i < count; i++) map[i] = i;
                _QuickSort(map, 0, count - 1);
                return map;
            }

            void _QuickSort(int[] map, int left, int right)
            {
                do
                {
                    int i = left;
                    int j = right;
                    int x = map[i + ((j - i) >> 1)];
                    do
                    {
                        while (i < map.Length && _CompareKeys(x, map[i]) > 0) i++;
                        while (j >= 0 && _CompareKeys(x, map[j]) < 0) j--;
                        if (i > j) break;
                        if (i < j)
                        {
                            int temp = map[i];
                            map[i] = map[j];
                            map[j] = temp;
                        }
                        i++;
                        j--;
                    } while (i <= j);
                    if (j - left <= right - i)
                    {
                        if (left < j) _QuickSort(map, left, j);
                        left = i;
                    }
                    else
                    {
                        if (i < right) _QuickSort(map, i, right);
                        right = j;
                    }
                } while (left < right);
            }
        }

        internal class _EnumerableSorter<TElement, TKey> : _EnumerableSorter<TElement>
        {
            internal Func<TElement, TKey> keySelector;
            internal IComparer<TKey> comparer;
            internal bool descending;
            internal _EnumerableSorter<TElement> next;
            internal TKey[] keys;

            internal _EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, _EnumerableSorter<TElement> next)
            {
                this.keySelector = keySelector;
                this.comparer = comparer;
                this.descending = descending;
                this.next = next;
            }

            internal override void _ComputeKeys(TElement[] elements, int count)
            {
                keys = new TKey[count];
                for (int i = 0; i < count; i++) keys[i] = keySelector(elements[i]);
                if (next != null) next._ComputeKeys(elements, count);
            }

            internal override int _CompareKeys(int index1, int index2)
            {
                int c = comparer.Compare(keys[index1], keys[index2]);
                if (c == 0)
                {
                    if (next == null) return index1 - index2;
                    return next._CompareKeys(index1, index2);
                }
                return descending ? -c : c;
            }
        }

        struct _Buffer<TElement>
        {
            internal TElement[] items;
            internal int count;

            internal _Buffer(IEnumerable<TElement> source)
            {
                TElement[] items = null;
                int count = 0;
                ICollection<TElement> collection = source as ICollection<TElement>;
                if (collection != null)
                {
                    count = collection.Count;
                    if (count > 0)
                    {
                        items = new TElement[count];
                        collection.CopyTo(items, 0);
                    }
                }
                else
                {
                    foreach (TElement item in source)
                    {
                        if (items == null)
                        {
                            items = new TElement[4];
                        }
                        else if (items.Length == count)
                        {
                            TElement[] newItems = new TElement[checked(count * 2)];
                            Array.Copy(items, 0, newItems, 0, count);
                            items = newItems;
                        }
                        items[count] = item;
                        count++;
                    }
                }
                this.items = items;
                this.count = count;
            }

            internal TElement[] _ToArray()
            {
                if (count == 0) return new TElement[0];
                if (items.Length == count) return items;
                TElement[] result = new TElement[count];
                Array.Copy(items, 0, result, 0, count);
                return result;
            }
        }

        /// <summary>
        /// This class provides the items view for the Enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal sealed class _SystemCore_EnumerableDebugView<T>
        {
            public _SystemCore_EnumerableDebugView(IEnumerable<T> enumerable)
            {
                if (enumerable == null)
                {
                    throw new ArgumentNullException("enumerable");
                }

                this.enumerable = enumerable;
            }

            public T[] Items
            {
                get
                {
                    List<T> tempList = new List<T>();
                    IEnumerator<T> currentEnumerator = this.enumerable.GetEnumerator();

                    if (currentEnumerator != null)
                    {
                        for (count = 0; currentEnumerator.MoveNext(); count++)
                        {
                            tempList.Add(currentEnumerator.Current);
                        }
                    }
                    if (count == 0)
                    {
                        throw new _SystemCore_EnumerableDebugViewEmptyException();
                    }
                    cachedCollection = new T[this.count];
                    tempList.CopyTo(cachedCollection, 0);
                    return cachedCollection;
                }
            }

            private IEnumerable<T> enumerable;

            private T[] cachedCollection;

            private int count;
        }

        internal sealed class _SystemCore_EnumerableDebugViewEmptyException : Exception
        {
            public string Empty
            {
                get
                {
                    return String.Empty;
                }
            }
        }

        internal sealed class _SystemCore_EnumerableDebugView
        {
            public _SystemCore_EnumerableDebugView(IEnumerable enumerable)
            {
                if (enumerable == null)
                {
                    throw new ArgumentNullException("enumerable");
                }

                this.enumerable = enumerable;
                count = 0;
                cachedCollection = null;
            }

            public object[] Items
            {
                get
                {
                    List<object> tempList = new List<object>();
                    IEnumerator currentEnumerator = this.enumerable.GetEnumerator();

                    if (currentEnumerator != null)
                    {
                        for (count = 0; currentEnumerator.MoveNext(); count++)
                        {
                            tempList.Add(currentEnumerator.Current);
                        }
                    }
                    if (count == 0)
                    {
                        throw new _SystemCore_EnumerableDebugViewEmptyException();
                    }
                    cachedCollection = new object[this.count];
                    tempList.CopyTo(cachedCollection, 0);
                    return cachedCollection;
                }
            }

            private IEnumerable enumerable;

            private object[] cachedCollection;

            private int count;
        }
    }
    

    /// <summary>
    /// Internal helper functions for working with enumerables.
    /// </summary>
    internal static partial class _EnumerableHelpers
    {
        /// <summary>
        /// Tries to get the count of the enumerable cheaply.
        /// </summary>
        /// <typeparam name="T">The element type of the source enumerable.</typeparam>
        /// <param name="source">The enumerable to count.</param>
        /// <param name="count">The count of the enumerable, if it could be obtained cheaply.</param>
        /// <returns><c>true if the enumerable could be counted cheaply; otherwise, <c>false</c>.
        internal static bool _TryGetCount<T>(IEnumerable<T> source, out int count)
        {
            Debug.Assert(source != null);

            if (source is ICollection<T> collection)
            {
                count = collection.Count;
                return true;
            }

            if (source is _IIListProvider<T> provider)
            {
                return (count = provider._GetCount(onlyIfCheap: true)) >= 0;
            }

            count = -1;
            return false;
        }
    }

    /// <summary>
    /// Internal helper functions for working with enumerables.
    /// </summary>
    internal static partial class _EnumerableHelpers
    {
        /// <summary>
        /// Copies items from an enumerable to an array.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the array to start copying to.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        internal static void _Copy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(source != null);
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(array?.Length - arrayIndex >= count);

            if (source is ICollection<T> collection)
            {
                Debug.Assert(collection.Count == count);
                collection.CopyTo(array, arrayIndex);
                return;
            }

            _IterativeCopy(source, array, arrayIndex, count);
        }

        /// <summary>
        /// Copies items from a non-collection enumerable to an array.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="source">The source enumerable.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in the array to start copying to.</param>
        /// <param name="count">The number of items in the enumerable.</param>
        internal static void _IterativeCopy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(source != null && !(source is ICollection<T>));
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(array?.Length - arrayIndex >= count);

            int endIndex = arrayIndex + count;
            foreach (T item in source)
            {
                array[arrayIndex++] = item;
            }

            Debug.Assert(arrayIndex == endIndex);
        }

        /// <summary>Converts an enumerable to an array.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <returns>The resulting array.</returns>
        internal static T[] _ToArray<T>(IEnumerable<T> source)
        {
            Debug.Assert(source != null);

            if (source is ICollection<T> collection)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return new T[0];
                }

                var result = new T[count];
                collection.CopyTo(result, arrayIndex: 0);
                return result;
            }

            var builder = new _LargeArrayBuilder<T>(initialize: true);
            builder._AddRange(source);
            return builder._ToArray();
        }

        /// <summary>Converts an enumerable to an array using the same logic as List{T}.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <param name="length">The number of items stored in the resulting array, 0-indexed.</param>
        /// <returns>
        /// The resulting array.  The length of the array may be greater than <paramref name="length"/>,
        /// which is the actual number of elements in the array.
        /// </returns>
        internal static T[] _ToArray<T>(IEnumerable<T> source, out int length)
        {
            if (source is ICollection<T> ic)
            {
                int count = ic.Count;
                if (count != 0)
                {
                    // Allocate an array of the desired size, then copy the elements into it. Note that this has the same
                    // issue regarding concurrency as other existing collections like List<T>. If the collection size
                    // concurrently changes between the array allocation and the CopyTo, we could end up either getting an
                    // exception from overrunning the array (if the size went up) or we could end up not filling as many
                    // items as 'count' suggests (if the size went down).  This is only an issue for concurrent collections
                    // that implement ICollection<T>, which as of .NET 4.6 is just ConcurrentDictionary<TKey, TValue>.
                    T[] arr = new T[count];
                    ic.CopyTo(arr, 0);
                    length = count;
                    return arr;
                }
            }
            else
            {
                using (var en = source.GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        const int DefaultCapacity = 4;
                        T[] arr = new T[DefaultCapacity];
                        arr[0] = en.Current;
                        int count = 1;

                        while (en.MoveNext())
                        {
                            if (count == arr.Length)
                            {
                                // MaxArrayLength is defined in Array.MaxArrayLength and in gchelpers in CoreCLR.
                                // It represents the maximum number of elements that can be in an array where
                                // the size of the element is greater than one byte; a separate, slightly larger constant,
                                // is used when the size of the element is one.
                                const int MaxArrayLength = 0x7FEFFFFF;

                                // This is the same growth logic as in List<T>:
                                // If the array is currently empty, we make it a default size.  Otherwise, we attempt to
                                // double the size of the array.  Doubling will overflow once the size of the array reaches
                                // 2^30, since doubling to 2^31 is 1 larger than Int32.MaxValue.  In that case, we instead
                                // constrain the length to be MaxArrayLength (this overflow check works because of the
                                // cast to uint).  Because a slightly larger constant is used when T is one byte in size, we
                                // could then end up in a situation where arr.Length is MaxArrayLength or slightly larger, such
                                // that we constrain newLength to be MaxArrayLength but the needed number of elements is actually
                                // larger than that.  For that case, we then ensure that the newLength is large enough to hold
                                // the desired capacity.  This does mean that in the very rare case where we've grown to such a
                                // large size, each new element added after MaxArrayLength will end up doing a resize.
                                int newLength = count << 1;
                                if ((uint)newLength > MaxArrayLength)
                                {
                                    newLength = MaxArrayLength <= count ? count + 1 : MaxArrayLength;
                                }

                                Array.Resize(ref arr, newLength);
                            }

                            arr[count++] = en.Current;
                        }

                        length = count;
                        return arr;
                    }
                }
            }

            length = 0;
            return new T[0];
        }
    }

    /// <summary>
    /// Represents a position within a <see cref="_LargeArrayBuilder{T}"/>.
    /// </summary>
    internal struct _CopyPosition
    {
        /// <summary>
        /// Constructs a new <see cref="_CopyPosition"/>.
        /// </summary>
        /// <param name="row">The index of the buffer to select.</param>
        /// <param name="column">The index within the buffer to select.</param>
        internal _CopyPosition(int row, int column)
        {
            Debug.Assert(row >= 0);
            Debug.Assert(column >= 0);

            Row = row;
            Column = column;
        }

        /// <summary>
        /// Represents a position at the start of a <see cref="_LargeArrayBuilder{T}"/>.
        /// </summary>
        public static _CopyPosition Start => default(_CopyPosition);

        /// <summary>
        /// The index of the buffer to select.
        /// </summary>
        internal int Row { get; }

        /// <summary>
        /// The index within the buffer to select.
        /// </summary>
        internal int Column { get; }

        /// <summary>
        /// If this position is at the end of the current buffer, returns the position
        /// at the start of the next buffer. Otherwise, returns this position.
        /// </summary>
        /// <param name="endColumn">The length of the current buffer.</param>
        public _CopyPosition Normalize(int endColumn)
        {
            Debug.Assert(Column <= endColumn);

            return Column == endColumn ?
                new _CopyPosition(Row + 1, 0) :
                this;
        }

        /// <summary>
        /// Gets a string suitable for display in the debugger.
        /// </summary>
        private string DebuggerDisplay => $"[{Row}, {Column}]";
    }

    /// <summary>
    /// Helper type for avoiding allocations while building arrays.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal struct _ArrayBuilder<T>
    {
        private const int DefaultCapacity = 4;
        private const int MaxCoreClrArrayLength = 0x7fefffff; // For byte arrays the limit is slightly larger

        private T[] _array; // Starts out null, initialized on first Add.
        private int _count; // Number of items into _array we're using.

        /// <summary>
        /// Initializes the <see cref="_ArrayBuilder{T}"/> with a specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the array to allocate.</param>
        public _ArrayBuilder(int capacity) : this()
        {
            Debug.Assert(capacity >= 0);
            if (capacity > 0)
            {
                _array = new T[capacity];
            }
        }

        /// <summary>
        /// Gets the number of items this instance can store without re-allocating,
        /// or 0 if the backing array is <c>null</c>.
        /// </summary>
        public int Capacity => _array?.Length ?? 0;

        /// <summary>
        /// Gets the number of items in the array currently in use.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets or sets the item at a certain index in the array.
        /// </summary>
        /// <param name="index">The index into the array.</param>
        public T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < _count);
                return _array[index];
            }
            set
            {
                Debug.Assert(index >= 0 && index < _count);
                _array[index] = value;
            }
        }

        /// <summary>
        /// Adds an item to the backing array, resizing it if necessary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void _Add(T item)
        {
            if (_count == Capacity)
            {
                _EnsureCapacity(_count + 1);
            }

            _UncheckedAdd(item);
        }

        /// <summary>
        /// Gets the first item in this builder.
        /// </summary>
        public T _First()
        {
            Debug.Assert(_count > 0);
            return _array[0];
        }

        /// <summary>
        /// Gets the last item in this builder.
        /// </summary>
        public T _Last()
        {
            Debug.Assert(_count > 0);
            return _array[_count - 1];
        }

        /// <summary>
        /// Creates an array from the contents of this builder.
        /// </summary>
        /// <remarks>
        /// Do not call this method twice on the same builder.
        /// </remarks>
        public T[] _ToArray()
        {
            if (_count == 0)
            {
                return new T[0];
            }

            Debug.Assert(_array != null); // Nonzero _count should imply this

            T[] result = _array;
            if (_count < result.Length)
            {
                // Avoid a bit of overhead (method call, some branches, extra codegen)
                // which would be incurred by using Array.Resize
                result = new T[_count];
                Array.Copy(_array, 0, result, 0, _count);
            }

#if DEBUG
            // Try to prevent callers from using the ArrayBuilder after ToArray, if _count != 0.
            _count = -1;
            _array = null;
#endif

            return result;
        }

        /// <summary>
        /// Adds an item to the backing array, without checking if there is room.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use this method if you know there is enough space in the <see cref="_ArrayBuilder{T}"/>
        /// for another item, and you are writing performance-sensitive code.
        /// </remarks>
        public void _UncheckedAdd(T item)
        {
            Debug.Assert(_count < Capacity);

            _array[_count++] = item;
        }

        private void _EnsureCapacity(int minimum)
        {
            Debug.Assert(minimum > Capacity);

            int capacity = Capacity;
            int nextCapacity = capacity == 0 ? DefaultCapacity : 2 * capacity;

            if ((uint)nextCapacity > (uint)MaxCoreClrArrayLength)
            {
                nextCapacity = Math.Max(capacity + 1, MaxCoreClrArrayLength);
            }

            nextCapacity = Math.Max(nextCapacity, minimum);

            T[] next = new T[nextCapacity];
            if (_count > 0)
            {
                Array.Copy(_array, 0, next, 0, _count);
            }
            _array = next;
        }
    }

    /// <summary>
    /// Helper type for building dynamically-sized arrays while minimizing allocations and copying.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal struct _LargeArrayBuilder<T>
    {
        private const int StartingCapacity = 4;
        private const int ResizeLimit = 8;

        private readonly int _maxCapacity;  // The maximum capacity this builder can have.
        private T[] _first;                 // The first buffer we store items in. Resized until ResizeLimit.
        private _ArrayBuilder<T[]> _buffers; // After ResizeLimit * 2, we store previous buffers we've filled out here.
        private T[] _current;               // Current buffer we're reading into. If _count <= ResizeLimit, this is _first.
        private int _index;                 // Index into the current buffer.
        private int _count;                 // Count of all of the items in this builder.

        /// <summary>
        /// Constructs a new builder.
        /// </summary>
        /// <param name="initialize">Pass <c>true.
        public _LargeArrayBuilder(bool initialize)
            : this(maxCapacity: int.MaxValue)
        {
            // This is a workaround for C# not having parameterless struct constructors yet.
            // Once it gets them, replace this with a parameterless constructor.
            Debug.Assert(initialize);
        }

        /// <summary>
        /// Constructs a new builder with the specified maximum capacity.
        /// </summary>
        /// <param name="maxCapacity">The maximum capacity this builder can have.</param>
        /// <remarks>
        /// Do not add more than <paramref name="maxCapacity"/> items to this builder.
        /// </remarks>
        public _LargeArrayBuilder(int maxCapacity)
            : this()
        {
            Debug.Assert(maxCapacity >= 0);

            _first = _current = new T[0];
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Gets the number of items added to the builder.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use <see cref="_Add"/> if adding to the builder is a bottleneck for your use case.
        /// Otherwise, use <see cref="_SlowAdd"/>.
        /// </remarks>
        public void _Add(T item)
        {
            Debug.Assert(_maxCapacity > _count);

            if (_index == _current.Length)
            {
                _AllocateBuffer();
            }

            _current[_index++] = item;
            _count++;
        }

        /// <summary>
        /// Adds a range of items to this builder.
        /// </summary>
        /// <param name="items">The sequence to add.</param>
        /// <remarks>
        /// It is the caller's responsibility to ensure that adding <paramref name="items"/>
        /// does not cause the builder to exceed its maximum capacity.
        /// </remarks>
        public void _AddRange(IEnumerable<T> items)
        {
            Debug.Assert(items != null);

            using (IEnumerator<T> enumerator = items.GetEnumerator())
            {
                T[] destination = _current;
                int index = _index;

                // Continuously read in items from the enumerator, updating _count
                // and _index when we run out of space.

                while (enumerator.MoveNext())
                {
                    if (index == destination.Length)
                    {
                        // No more space in this buffer. Resize.
                        _count += index - _index;
                        _index = index;
                        _AllocateBuffer();
                        destination = _current;
                        index = _index; // May have been reset to 0
                    }

                    destination[index++] = enumerator.Current;
                }

                // Final update to _count and _index.
                _count += index - _index;
                _index = index;
            }
        }

        /// <summary>
        /// Copies the contents of this builder to the specified array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in <see cref="array"/> to start copying to.
        /// <param name="count">The number of items to copy.</param>
        public void _CopyTo(T[] array, int arrayIndex, int count)
        {
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count >= 0 && count <= Count);
            Debug.Assert(array?.Length - arrayIndex >= count);

            for (int i = 0; count > 0; i++)
            {
                // Find the buffer we're copying from.
                T[] buffer = _GetBuffer(index: i);

                // Copy until we satisfy count, or we reach the end of the buffer.
                int toCopy = Math.Min(count, buffer.Length);
                Array.Copy(buffer, 0, array, arrayIndex, toCopy);

                // Increment variables to that position.
                count -= toCopy;
                arrayIndex += toCopy;
            }
        }

        /// <summary>
        /// Copies the contents of this builder to the specified array.
        /// </summary>
        /// <param name="position">The position in this builder to start copying from.</param>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The index in <see cref="array"/> to start copying to.
        /// <param name="count">The number of items to copy.</param>
        /// <returns>The position in this builder that was copied up to.</returns>
        public _CopyPosition _CopyTo(_CopyPosition position, T[] array, int arrayIndex, int count)
        {
            Debug.Assert(array != null);
            Debug.Assert(arrayIndex >= 0);
            Debug.Assert(count > 0 && count <= Count);
            Debug.Assert(array.Length - arrayIndex >= count);

            // Go through each buffer, which contains one 'row' of items.
            // The index in each buffer is referred to as the 'column'.

            /*
             * Visual representation:
             * 
             *       C0   C1   C2 ..  C31 ..   C63
             * R0:  [0]  [1]  [2] .. [31]
             * R1: [32] [33] [34] .. [63]
             * R2: [64] [65] [66] .. [95] .. [127]
             */

            int row = position.Row;
            int column = position.Column;

            T[] buffer = _GetBuffer(row);
            int copied = _CopyToCore(buffer, column);

            if (count == 0)
            {
                return new _CopyPosition(row, column + copied).Normalize(buffer.Length);
            }

            do
            {
                buffer = _GetBuffer(++row);
                copied = _CopyToCore(buffer, 0);
            } while (count > 0);

            return new _CopyPosition(row, copied).Normalize(buffer.Length);

            int _CopyToCore(T[] sourceBuffer, int sourceIndex)
            {
                Debug.Assert(sourceBuffer.Length > sourceIndex);

                // Copy until we satisfy `count` or reach the end of the current buffer.
                int copyCount = Math.Min(sourceBuffer.Length - sourceIndex, count);
                Array.Copy(sourceBuffer, sourceIndex, array, arrayIndex, copyCount);

                arrayIndex += copyCount;
                count -= copyCount;

                return copyCount;
            }
        }

        /// <summary>
        /// Retrieves the buffer at the specified index.
        /// </summary>
        /// <param name="index">The index of the buffer.</param>
        public T[] _GetBuffer(int index)
        {
            Debug.Assert(index >= 0 && index < _buffers.Count + 2);

            return index == 0 ? _first :
                index <= _buffers.Count ? _buffers[index - 1] :
                    _current;
        }

        /// <summary>
        /// Adds an item to this builder.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// Use <see cref="_Add"/> if adding to the builder is a bottleneck for your use case.
        /// Otherwise, use <see cref="_SlowAdd"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void _SlowAdd(T item) => _Add(item);

        /// <summary>
        /// Creates an array from the contents of this builder.
        /// </summary>
        public T[] _ToArray()
        {
            if (_TryMove(out T[] array))
            {
                // No resizing to do.
                return array;
            }

            array = new T[_count];
            _CopyTo(array, 0, _count);
            return array;
        }

        /// <summary>
        /// Attempts to transfer this builder into an array without copying.
        /// </summary>
        /// <param name="array">The transferred array, if the operation succeeded.</param>
        /// <returns><c>true if the operation succeeded; otherwise, <c>false</c>.
        public bool _TryMove(out T[] array)
        {
            array = _first;
            return _count == _first.Length;
        }

        private void _AllocateBuffer()
        {
            // - On the first few adds, simply resize _first.
            // - When we pass ResizeLimit, allocate ResizeLimit elements for _current
            //   and start reading into _current. Set _index to 0.
            // - When _current runs out of space, add it to _buffers and repeat the
            //   above step, except with _current.Length * 2.
            // - Make sure we never pass _maxCapacity in all of the above steps.

            Debug.Assert((uint)_maxCapacity > (uint)_count);
            Debug.Assert(_index == _current.Length, $"{nameof(_AllocateBuffer)} was called, but there's more space.");

            // If _count is int.MinValue, we want to go down the other path which will raise an exception.
            if ((uint)_count < (uint)ResizeLimit)
            {
                // We haven't passed ResizeLimit. Resize _first, copying over the previous items.
                Debug.Assert(_current == _first && _count == _first.Length);

                int nextCapacity = Math.Min(_count == 0 ? StartingCapacity : _count * 2, _maxCapacity);

                _current = new T[nextCapacity];
                Array.Copy(_first, 0, _current, 0, _count);
                _first = _current;
            }
            else
            {
                Debug.Assert(_maxCapacity > ResizeLimit);
                Debug.Assert(_count == ResizeLimit ^ _current != _first);

                int nextCapacity;
                if (_count == ResizeLimit)
                {
                    nextCapacity = ResizeLimit;
                }
                else
                {
                    // Example scenario: Let's say _count == 64.
                    // Then our buffers look like this: | 8 | 8 | 16 | 32 |
                    // As you can see, our count will be just double the last buffer.
                    // Now, say _maxCapacity is 100. We will find the right amount to allocate by
                    // doing min(64, 100 - 64). The lhs represents double the last buffer,
                    // the rhs the limit minus the amount we've already allocated.

                    Debug.Assert(_count >= ResizeLimit * 2);
                    Debug.Assert(_count == _current.Length * 2);

                    _buffers._Add(_current);
                    nextCapacity = Math.Min(_count, _maxCapacity - _count);
                }

                _current = new T[nextCapacity];
                _index = 0;
            }
        }
    }
}

namespace System.Linq
{
    internal static partial class _Enumerable
    {
        public static TSource[] _ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source is _IIListProvider<TSource> arrayProvider
                ? arrayProvider._ToArray()
                : _EnumerableHelpers._ToArray(source);
        }

        public static List<TSource> _ToList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source is _IIListProvider<TSource> listProvider ? listProvider._ToList() : new List<TSource>(source);
        }

    }

    /// <summary>
    /// An iterator that can produce an array or <see cref="List{TElement}"/> through an optimized path.
    /// </summary>
    internal interface _IIListProvider<TElement> : IEnumerable<TElement>
    {
        /// <summary>
        /// Produce an array of the sequence through an optimized path.
        /// </summary>
        /// <returns>The array.</returns>
        TElement[] _ToArray();

        /// <summary>
        /// Produce a <see cref="List{TElement}"/> of the sequence through an optimized path.
        /// </summary>
        /// <returns>The <see cref="List{TElement}"/>.
        List<TElement> _ToList();

        /// <summary>
        /// Returns the count of elements in the sequence.
        /// </summary>
        /// <param name="onlyIfCheap">If true then the count should only be calculated if doing
        /// so is quick (sure or likely to be constant time), otherwise -1 should be returned.</param>
        /// <returns>The number of elements.</returns>
        int _GetCount(bool onlyIfCheap);
    }
}

