using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AirBreather.Core.Collections
{
    public sealed class BitList : IList<bool>, IReadOnlyList<bool>
    {
        private readonly List<int> values;

        private int version;

        public BitList()
        {
            this.values = new List<int>();
        }

        private BitList(IEnumerable<int> values)
        {
            this.values = values.ToList();
        }

        public bool Remove(bool item)
        {
            int index = this.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            this.RemoveAt(index);
            return true;
        }

        public int Count { get; private set; }

        public bool IsReadOnly { get { return false; } }

        public int IndexOf(bool item)
        {
            var xyz = this.Select((x, idx) => new { X = x, Idx = idx }).FirstOrDefault(x => x.X == item);
            return xyz == null ? -1 : xyz.Idx;
        }

        public void Insert(int index, bool item)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", index, "Must be non-negative.");
            }

            if (this.Count < index)
            {
                throw new ArgumentOutOfRangeException("index", index, "Too big.");
            }

            if (this.Count == index)
            {
                this.AddCore(item);
            }
            else
            {
                // TODO: optimize this
                BitList copied = new BitList(this.values) { Count = this.Count };
                this.values.Clear();
                this.Count = 0;
                for (int i = 0; i <= copied.Count; i++)
                {
                    bool next;

                    if (i == index)
                    {
                        next = item;
                    }
                    else if (i > index)
                    {
                        next = copied[i - 1];
                    }
                    else
                    {
                        next = copied[i];
                    }

                    this.AddCore(next);
                }
            }

            this.version++;
        }

        public void RemoveAt(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index", index, "Must be non-negative.");
            }

            if (this.Count <= index)
            {
                throw new ArgumentOutOfRangeException("index", index, "Too big.");
            }

            // TODO: optimize this
            BitList copied = new BitList(this.values) { Count = this.Count };
            this.values.Clear();
            this.Count = 0;
            for (int i = 0; i < copied.Count; i++)
            {
                if (i == index)
                {
                    continue;
                }

                this.AddCore(copied[i]);
            }

            this.version++;
        }

        public bool this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index", index, "Must be non-negative.");
                }

                if (this.Count <= index)
                {
                    throw new ArgumentOutOfRangeException("index", index, "Too big.");
                }

                return (this.values[index / 32] & (1u << (index % 32))) > 0;
            }

            set
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index", index, "Must be non-negative.");
                }

                if (this.Count <= index)
                {
                    throw new ArgumentOutOfRangeException("index", index, "Too big.");
                }

                int mask = unchecked((int)1u << (index % 32));
                if (value)
                {
                    this.values[index / 32] |= mask;
                }
                else
                {
                    this.values[index / 32] &= ~mask;
                }

                this.version++;
            }
        }

        public void Add(bool value)
        {
            this.AddCore(value);
            this.version++;
        }

        public void Clear()
        {
            this.values.Clear();
            this.Count = 0;
            this.version++;
        }

        public bool Contains(bool item)
        {
            // TODO: optimize, we only need to go bit-by-bit for the last int.
            return this.Any(x => x == item);
        }

        public void CopyTo(bool[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "Must be non-negative.");
            }

            if (array.Length <= arrayIndex)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "Must be less than the length of the array.");
            }

            if (array.Length - arrayIndex < this.Count)
            {
                throw new ArgumentException("Not enough room", "array");
            }

            for (int i = 0; i < this.Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        public BitArray ToBitArray()
        {
            return new BitArray(this.values.ToArray())
            {
                Length = this.Count
            };
        }

        public void TrimExcess()
        {
            this.values.TrimExcess();
        }

        public IEnumerator<bool> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void AddCore(bool value)
        {
            int offset = this.Count % 32;
            if (offset == 0)
            {
                this.values.Add(0);
            }

            if (value)
            {
                this.values[this.values.Count - 1] |= (1 << offset);
            }

            this.Count++;
        }

        private sealed class Enumerator : IEnumerator<bool>
        {
            private readonly BitList lst;

            private readonly int version;

            private int currIndex;

            private bool curr;

            internal Enumerator(BitList lst)
            {
                this.lst = lst;
                this.version = lst.version;
                this.currIndex = 0;
                this.curr = false;
            }

            public bool Current
            {
                get { return this.curr; }
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                if (this.version != this.lst.version)
                {
                    throw new InvalidOperationException("Collection was modified during enumeration.");
                }

                if (this.lst.Count <= this.currIndex)
                {
                    return false;
                }

                this.curr = this.lst[this.currIndex++];
                return true;
            }

            public void Reset()
            {
                if (this.version != this.lst.version)
                {
                    throw new InvalidOperationException("Collection was modified during enumeration.");
                }

                this.currIndex = 0;
            }

            public void Dispose()
            {
            }
        }
    }
}
