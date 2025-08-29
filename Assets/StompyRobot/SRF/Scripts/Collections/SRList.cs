namespace SRF
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using UnityEngine;

    /// <summary>
    /// IList implementation which does not release the buffer when clearing/removing elements. Based on the NGUI BetterList
    /// </summary>
    [Serializable]
    public class SRList<T> : IList<T>, ISerializationCallbackReceiver
    {
        [SerializeField] private T[] _buffer;
        [SerializeField] private int _count;
        private EqualityComparer<T> _equalityComparer;
        private ReadOnlyCollection<T> _readOnlyWrapper;
        public SRList() { }

        public SRList(int capacity)
        {
            this.Buffer = new T[capacity];
        }

        /// <summary>
        /// Create a new list with the range of values. Contains a foreach loop, which will allocate garbage when used with most
        /// generic collection types.
        /// </summary>
        public SRList(IEnumerable<T> source)
        {
            this.AddRange(source);
        }

        public T[] Buffer
        {
            get { return this._buffer; }
            private set { this._buffer = value; }
        }

        private EqualityComparer<T> EqualityComparer
        {
            get
            {
                if (this._equalityComparer == null)
                {
                    this._equalityComparer = EqualityComparer<T>.Default;
                }

                return this._equalityComparer;
            }
        }

        public int Count
        {
            get { return this._count; }
            private set { this._count = value; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (this.Buffer != null)
            {
                for (var i = 0; i < this.Count; ++i)
                {
                    yield return this.Buffer[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Add(T item)
        {
            if (this.Buffer == null || this.Count == this.Buffer.Length)
            {
                this.Expand();
            }

            this.Buffer[this.Count++] = item;
        }

        public void Clear()
        {
            this.Count = 0;
        }

        public bool Contains(T item)
        {
            if (this.Buffer == null)
            {
                return false;
            }

            for (var i = 0; i < this.Count; ++i)
            {
                if (this.EqualityComparer.Equals(this.Buffer[i], item))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Trim();
            this.Buffer.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (this.Buffer == null)
            {
                return false;
            }

            var index = this.IndexOf(item);

            if (index < 0)
            {
                return false;
            }

            this.RemoveAt(index);

            return true;
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(T item)
        {
            if (this.Buffer == null)
            {
                return -1;
            }

            for (var i = 0; i < this.Count; ++i)
            {
                if (this.EqualityComparer.Equals(this.Buffer[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Insert(int index, T item)
        {
            if (this.Buffer == null || this.Count == this.Buffer.Length)
            {
                this.Expand();
            }

            if (index < this.Count)
            {
                for (var i = this.Count; i > index; --i)
                {
                    this.Buffer[i] = this.Buffer[i - 1];
                }
                this.Buffer[index] = item;
                ++this.Count;
            }
            else
            {
                this.Add(item);
            }
        }

        public void RemoveAt(int index)
        {
            if (this.Buffer != null && index < this.Count)
            {
                --this.Count;
                this.Buffer[index] = default(T);
                for (var b = index; b < this.Count; ++b)
                {
                    this.Buffer[b] = this.Buffer[b + 1];
                }
            }
        }

        public T this[int index]
        {
            get
            {
                if (this.Buffer == null)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.Buffer[index];
            }
            set
            {
                if (this.Buffer == null)
                {
                    throw new IndexOutOfRangeException();
                }

                this.Buffer[index] = value;
            }
        }

        public void OnBeforeSerialize()
        {
            // Clean buffer of unused elements before serializing
            this.Clean();
        }

        public void OnAfterDeserialize()
        {
        }

        /// <summary>
        /// Add range of values to the list. Contains a foreach loop, which will allocate garbage when used with most
        /// generic collection types.
        /// </summary>
        /// <param name="range"></param>
        public void AddRange(IEnumerable<T> range)
        {
            foreach (var item in range)
            {
                this.Add(item);
            }
        }

        /// <summary>
        /// Clear the list, optionally setting each element to default(T)
        /// </summary>
        public void Clear(bool clean)
        {
            this.Clear();

            if (!clean)
            {
                return;
            }

            this.Clean();
        }

        public void Clean()
        {
            if (this.Buffer == null)
            {
                return;
            }

            for (var i = this.Count; i < this._buffer.Length; i++)
            {
                this._buffer[i] = default(T);
            }
        }

        /// <summary>
        /// Get a read-only wrapper of this list. This is cached, so very little cost after first called.
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (this._readOnlyWrapper == null)
            {
                this._readOnlyWrapper = new ReadOnlyCollection<T>(this);
            }

            return this._readOnlyWrapper;
        }

        /// <summary>
        /// Helper function that expands the size of the array, maintaining the content.
        /// </summary>
        private void Expand()
        {
            var newList = (this.Buffer != null) ? new T[Mathf.Max(this.Buffer.Length << 1, 32)] : new T[32];

            if (this.Buffer != null && this.Count > 0)
            {
                this.Buffer.CopyTo(newList, 0);
            }

            this.Buffer = newList;
        }

        /// <summary>
        /// Trim the unnecessary memory, resizing the buffer to be of 'Length' size.
        /// Call this function only if you are sure that the buffer won't need to resize anytime soon.
        /// </summary>
        public void Trim()
        {
            if (this.Count > 0)
            {
                if (this.Count >= this.Buffer.Length)
                {
                    return;
                }

                var newList = new T[this.Count];

                for (var i = 0; i < this.Count; ++i)
                {
                    newList[i] = this.Buffer[i];
                }

                this.Buffer = newList;
            }
            else
            {
                this.Buffer = new T[0];
            }
        }

        /// <summary>
        /// List.Sort equivalent.
        /// </summary>
        public void Sort(Comparison<T> comparer)
        {
            var changed = true;

            while (changed)
            {
                changed = false;

                for (var i = 1; i < this.Count; ++i)
                {
                    if (comparer.Invoke(this.Buffer[i - 1], this.Buffer[i]) > 0)
                    {
                        var temp = this.Buffer[i];
                        this.Buffer[i] = this.Buffer[i - 1];
                        this.Buffer[i - 1] = temp;
                        changed = true;
                    }
                }
            }
        }
    }
}
