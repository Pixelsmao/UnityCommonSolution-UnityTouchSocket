//
// System.Collections.Generic.List
//
// Authors:
//    Ben Maurer (bmaurer@ximian.com)
//    Martin Baulig (martin@ximian.com)
//    Carlos Alberto Cortez (calberto.cortez@gmail.com)
//    David Waite (mass@akuma.org)
//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005 David Waite
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Spine
{
    [DebuggerDisplay("Count={Count}")]
    public class ExposedList<T> : IEnumerable<T>
    {
        public T[] Items;
        public int Count;
        private const int DefaultCapacity = 4;
        private static readonly T[] EmptyArray = new T[0];
        private int version;

        public ExposedList()
        {
            this.Items = EmptyArray;
        }

        public ExposedList(IEnumerable<T> collection)
        {
            this.CheckCollection(collection);

            // initialize to needed size (if determinable)
            var c = collection as ICollection<T>;
            if (c == null)
            {
                this.Items = EmptyArray;
                this.AddEnumerable(collection);
            }
            else
            {
                this.Items = new T[c.Count];
                this.AddCollection(c);
            }
        }

        public ExposedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            this.Items = new T[capacity];
        }

        internal ExposedList(T[] data, int size)
        {
            this.Items = data;
            this.Count = size;
        }

        public void Add(T item)
        {
            // If we check to see if we need to grow before trying to grow
            // we can speed things up by 25%
            if (this.Count == this.Items.Length)
                this.GrowIfNeeded(1);
            this.Items[this.Count++] = item;
            this.version++;
        }

        public void GrowIfNeeded(int addedCount)
        {
            var minimumSize = this.Count + addedCount;
            if (minimumSize > this.Items.Length)
                this.Capacity = Math.Max(Math.Max(this.Capacity * 2, DefaultCapacity), minimumSize);
        }

        public ExposedList<T> Resize(int newSize)
        {
            var itemsLength = this.Items.Length;
            var oldItems = this.Items;
            if (newSize > itemsLength)
            {
                Array.Resize(ref this.Items, newSize);
                //				var newItems = new T[newSize];
                //				Array.Copy(oldItems, newItems, Count);
                //				Items = newItems;
            }
            else if (newSize < itemsLength)
            {
                // Allow nulling of T reference type to allow GC.
                for (var i = newSize; i < itemsLength; i++)
                    oldItems[i] = default(T);
            }
            this.Count = newSize;
            return this;
        }

        public void EnsureCapacity(int min)
        {
            if (this.Items.Length < min)
            {
                var newCapacity = this.Items.Length == 0 ? DefaultCapacity : this.Items.Length * 2;
                //if ((uint)newCapacity > Array.MaxArrayLength) newCapacity = Array.MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                this.Capacity = newCapacity;
            }
        }

        private void CheckRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if ((uint)index + (uint)count > (uint)this.Count)
                throw new ArgumentException("index and count exceed length of list");
        }

        private void AddCollection(ICollection<T> collection)
        {
            var collectionCount = collection.Count;
            if (collectionCount == 0)
                return;

            this.GrowIfNeeded(collectionCount);
            collection.CopyTo(this.Items, this.Count);
            this.Count += collectionCount;
        }

        private void AddEnumerable(IEnumerable<T> enumerable)
        {
            foreach (var t in enumerable)
            {
                this.Add(t);
            }
        }

        // Additional overload provided because ExposedList<T> only implements IEnumerable<T>,
        // leading to sub-optimal behavior: It grows multiple times as it assumes not
        // to know the final size ahead of insertion.
        public void AddRange(ExposedList<T> list)
        {
            this.CheckCollection(list);

            var collectionCount = list.Count;
            if (collectionCount == 0)
                return;

            this.GrowIfNeeded(collectionCount);
            list.CopyTo(this.Items, this.Count);
            this.Count += collectionCount;

            this.version++;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            this.CheckCollection(collection);

            var c = collection as ICollection<T>;
            if (c != null)
                this.AddCollection(c);
            else
                this.AddEnumerable(collection);
            this.version++;
        }

        public int BinarySearch(T item)
        {
            return Array.BinarySearch<T>(this.Items, 0, this.Count, item);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return Array.BinarySearch<T>(this.Items, 0, this.Count, item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            this.CheckRange(index, count);
            return Array.BinarySearch<T>(this.Items, index, count, item, comparer);
        }

        public void Clear(bool clearArray = true)
        {
            if (clearArray)
                Array.Clear(this.Items, 0, this.Items.Length);

            this.Count = 0;
            this.version++;
        }

        public bool Contains(T item)
        {
            return Array.IndexOf<T>(this.Items, item, 0, this.Count) != -1;
        }

        public ExposedList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
                throw new ArgumentNullException("converter");
            var u = new ExposedList<TOutput>(this.Count);
            for (var i = 0; i < this.Count; i++)
                u.Items[i] = converter(this.Items[i]);

            u.Count = this.Count;
            return u;
        }

        public void CopyTo(T[] array)
        {
            Array.Copy(this.Items, 0, array, 0, this.Count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(this.Items, 0, array, arrayIndex, this.Count);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            this.CheckRange(index, count);
            Array.Copy(this.Items, index, array, arrayIndex, count);
        }



        public bool Exists(Predicate<T> match)
        {
            CheckMatch(match);
            return this.GetIndex(0, this.Count, match) != -1;
        }

        public T Find(Predicate<T> match)
        {
            CheckMatch(match);
            var i = this.GetIndex(0, this.Count, match);
            return (i != -1) ? this.Items[i] : default(T);
        }

        private static void CheckMatch(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");
        }

        public ExposedList<T> FindAll(Predicate<T> match)
        {
            CheckMatch(match);
            return this.FindAllList(match);
        }

        private ExposedList<T> FindAllList(Predicate<T> match)
        {
            var results = new ExposedList<T>();
            for (var i = 0; i < this.Count; i++)
                if (match(this.Items[i]))
                    results.Add(this.Items[i]);

            return results;
        }

        public int FindIndex(Predicate<T> match)
        {
            CheckMatch(match);
            return this.GetIndex(0, this.Count, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            CheckMatch(match);
            this.CheckIndex(startIndex);
            return this.GetIndex(startIndex, this.Count - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            CheckMatch(match);
            this.CheckRange(startIndex, count);
            return this.GetIndex(startIndex, count, match);
        }

        private int GetIndex(int startIndex, int count, Predicate<T> match)
        {
            var end = startIndex + count;
            for (var i = startIndex; i < end; i++)
                if (match(this.Items[i]))
                    return i;

            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            CheckMatch(match);
            var i = this.GetLastIndex(0, this.Count, match);
            return i == -1 ? default(T) : this.Items[i];
        }

        public int FindLastIndex(Predicate<T> match)
        {
            CheckMatch(match);
            return this.GetLastIndex(0, this.Count, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            CheckMatch(match);
            this.CheckIndex(startIndex);
            return this.GetLastIndex(0, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            CheckMatch(match);
            var start = startIndex - count + 1;
            this.CheckRange(start, count);
            return this.GetLastIndex(start, count, match);
        }

        private int GetLastIndex(int startIndex, int count, Predicate<T> match)
        {
            // unlike FindLastIndex, takes regular params for search range
            for (var i = startIndex + count; i != startIndex;)
                if (match(this.Items[--i]))
                    return i;
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            for (var i = 0; i < this.Count; i++)
                action(this.Items[i]);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public ExposedList<T> GetRange(int index, int count)
        {
            this.CheckRange(index, count);
            var tmpArray = new T[count];
            Array.Copy(this.Items, index, tmpArray, 0, count);
            return new ExposedList<T>(tmpArray, count);
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf<T>(this.Items, item, 0, this.Count);
        }

        public int IndexOf(T item, int index)
        {
            this.CheckIndex(index);
            return Array.IndexOf<T>(this.Items, item, index, this.Count - index);
        }

        public int IndexOf(T item, int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if ((uint)index + (uint)count > (uint)this.Count)
                throw new ArgumentOutOfRangeException("index and count exceed length of list");

            return Array.IndexOf<T>(this.Items, item, index, count);
        }

        private void Shift(int start, int delta)
        {
            if (delta < 0)
                start -= delta;

            if (start < this.Count)
                Array.Copy(this.Items, start, this.Items, start + delta, this.Count - start);

            this.Count += delta;

            if (delta < 0)
                Array.Clear(this.Items, this.Count, -delta);
        }

        private void CheckIndex(int index)
        {
            if (index < 0 || (uint)index > (uint)this.Count)
                throw new ArgumentOutOfRangeException("index");
        }

        public void Insert(int index, T item)
        {
            this.CheckIndex(index);
            if (this.Count == this.Items.Length)
                this.GrowIfNeeded(1);
            this.Shift(index, 1);
            this.Items[index] = item;
            this.version++;
        }

        private void CheckCollection(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            this.CheckCollection(collection);
            this.CheckIndex(index);
            if (collection == this)
            {
                var buffer = new T[this.Count];
                this.CopyTo(buffer, 0);
                this.GrowIfNeeded(this.Count);
                this.Shift(index, buffer.Length);
                Array.Copy(buffer, 0, this.Items, index, buffer.Length);
            }
            else
            {
                var c = collection as ICollection<T>;
                if (c != null)
                    this.InsertCollection(index, c);
                else
                    this.InsertEnumeration(index, collection);
            }
            this.version++;
        }

        private void InsertCollection(int index, ICollection<T> collection)
        {
            var collectionCount = collection.Count;
            this.GrowIfNeeded(collectionCount);

            this.Shift(index, collectionCount);
            collection.CopyTo(this.Items, index);
        }

        private void InsertEnumeration(int index, IEnumerable<T> enumerable)
        {
            foreach (var t in enumerable)
                this.Insert(index++, t);
        }

        public int LastIndexOf(T item)
        {
            return Array.LastIndexOf<T>(this.Items, item, this.Count - 1, this.Count);
        }

        public int LastIndexOf(T item, int index)
        {
            this.CheckIndex(index);
            return Array.LastIndexOf<T>(this.Items, item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index", index, "index is negative");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count", count, "count is negative");

            if (index - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count", count, "count is too large");

            return Array.LastIndexOf<T>(this.Items, item, index, count);
        }

        public bool Remove(T item)
        {
            var loc = this.IndexOf(item);
            if (loc != -1)
                this.RemoveAt(loc);

            return loc != -1;
        }

        public int RemoveAll(Predicate<T> match)
        {
            CheckMatch(match);
            var i = 0;
            var j = 0;

            // Find the first item to remove
            for (i = 0; i < this.Count; i++)
                if (match(this.Items[i]))
                    break;

            if (i == this.Count)
                return 0;

            this.version++;

            // Remove any additional items
            for (j = i + 1; j < this.Count; j++)
            {
                if (!match(this.Items[j]))
                    this.Items[i++] = this.Items[j];
            }
            if (j - i > 0)
                Array.Clear(this.Items, i, j - i);

            this.Count = i;
            return (j - i);
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || (uint)index >= (uint)this.Count)
                throw new ArgumentOutOfRangeException("index");
            this.Shift(index, -1);
            Array.Clear(this.Items, this.Count, 1);
            this.version++;
        }

        // Spine Added Method
        // Based on Stack<T>.Pop(); https://referencesource.microsoft.com/#mscorlib/system/collections/stack.cs
        /// <summary>Pops the last item of the list. If the list is empty, Pop throws an InvalidOperationException.</summary>
        public T Pop()
        {
            if (this.Count == 0)
                throw new InvalidOperationException("List is empty. Nothing to pop.");

            var i = this.Count - 1;
            var item = this.Items[i];
            this.Items[i] = default(T);
            this.Count--;
            this.version++;
            return item;
        }

        public void RemoveRange(int index, int count)
        {
            this.CheckRange(index, count);
            if (count > 0)
            {
                this.Shift(index, -count);
                Array.Clear(this.Items, this.Count, count);
                this.version++;
            }
        }

        public void Reverse()
        {
            Array.Reverse(this.Items, 0, this.Count);
            this.version++;
        }

        public void Reverse(int index, int count)
        {
            this.CheckRange(index, count);
            Array.Reverse(this.Items, index, count);
            this.version++;
        }

        public void Sort()
        {
            Array.Sort<T>(this.Items, 0, this.Count, Comparer<T>.Default);
            this.version++;
        }

        public void Sort(IComparer<T> comparer)
        {
            Array.Sort<T>(this.Items, 0, this.Count, comparer);
            this.version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            Array.Sort<T>(this.Items, comparison);
            this.version++;
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            this.CheckRange(index, count);
            Array.Sort<T>(this.Items, index, count, comparer);
            this.version++;
        }

        public T[] ToArray()
        {
            var t = new T[this.Count];
            Array.Copy(this.Items, t, this.Count);

            return t;
        }

        public void TrimExcess()
        {
            this.Capacity = this.Count;
        }

        public bool TrueForAll(Predicate<T> match)
        {
            CheckMatch(match);

            for (var i = 0; i < this.Count; i++)
                if (!match(this.Items[i]))
                    return false;

            return true;
        }

        public int Capacity
        {
            get
            {
                return this.Items.Length;
            }
            set
            {
                if ((uint)value < (uint)this.Count)
                    throw new ArgumentOutOfRangeException();

                Array.Resize(ref this.Items, value);
            }
        }

        #region Interface implementations.

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        public struct Enumerator : IEnumerator<T>, IDisposable
        {
            private ExposedList<T> l;
            private int next;
            private readonly int ver;
            private T current;

            internal Enumerator(ExposedList<T> l)
                : this()
            {
                this.l = l;
                this.ver = l.version;
            }

            public void Dispose()
            {
                this.l = null;
            }

            private void VerifyState()
            {
                if (this.l == null)
                    throw new ObjectDisposedException(this.GetType().FullName);
                if (this.ver != this.l.version)
                    throw new InvalidOperationException(
                            "Collection was modified; enumeration operation may not execute.");
            }

            public bool MoveNext()
            {
                this.VerifyState();

                if (this.next < 0)
                    return false;

                if (this.next < this.l.Count)
                {
                    this.current = this.l.Items[this.next++];
                    return true;
                }

                this.next = -1;
                return false;
            }

            public T Current
            {
                get
                {
                    return this.current;
                }
            }

            void IEnumerator.Reset()
            {
                this.VerifyState();
                this.next = 0;
            }

            object IEnumerator.Current
            {
                get
                {
                    this.VerifyState();
                    if (this.next <= 0)
                        throw new InvalidOperationException();
                    return this.current;
                }
            }
        }
    }
}
