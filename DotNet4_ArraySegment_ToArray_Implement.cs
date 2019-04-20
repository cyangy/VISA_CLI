using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// This code was written by Thomas Levesque from https://stackoverflow.com/questions/12865611/shallow-copy-a-segment-of-a-value-type-array/12865641#12865641
// And  https://pastebin.com/cRcpBemQ
// Thanks to him
// For .NET Framework 4.0 :
/*
 * String s = "Hello Word! This is A text to test .Array  of   ArraySegment<T> on .NET Framework 4.0";
 *  byte[] myArr = Encoding.ASCII.GetBytes(s); //https://stackoverflow.com/questions/16072709/converting-string-to-byte-array-in-c-sharp
 *  short skip_n_bytes = 2;
 * ArraySegment<byte> segment = new ArraySegment<byte>(myArr, skip_n_bytes, myArr.Length - skip_n_bytes);
 *  segment.Array;  // This will always get the whole array instead of specified segment because of https://docs.microsoft.com/en-us/dotnet/api/system.arraysegment-1.-ctor?view=netframework-4.0
 * 
 */
namespace DotNet4_ArraySegment_ToArray_Implement
{
    public static class ArraySegmentExtensions
    {
        public static IList<T> AsList<T>(this ArraySegment<T> arraySegment)
        {
            return new ArraySegmentList<T>(arraySegment);
        }

        private class ArraySegmentList<T> : IList<T>
        {
            private readonly ArraySegment<T> _segment;

            public ArraySegmentList(ArraySegment<T> segment)
            {
                _segment = segment;
            }
            
            #region Implementation of IEnumerable

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Implementation of ICollection<T>

            public void Add(T item)
            {
                throw FixedLengthException();
            }

            public void Clear()
            {
                throw FixedLengthException();
            }

            public bool Contains(T item)
            {
                return this.AsEnumerable().Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                Array.Copy(_segment.Array, _segment.Offset, array, arrayIndex, _segment.Count);
            }

            public bool Remove(T item)
            {
                throw FixedLengthException();
            }

            public int Count { get { return _segment.Count; } }
            public bool IsReadOnly { get { return false; } }

            #endregion

            #region Implementation of IList<T>

            public int IndexOf(T item)
            {
                int arrayIndex = Array.IndexOf(_segment.Array, item, _segment.Offset, _segment.Count);
                if (arrayIndex != -1)
                    return arrayIndex - _segment.Offset;
                return -1;
            }

            public void Insert(int index, T item)
            {
                throw FixedLengthException();
            }

            public void RemoveAt(int index)
            {
                throw FixedLengthException();
            }

            public T this[int index]
            {
                get { return _segment.Array[index + _segment.Offset]; }
                set { _segment.Array[index + _segment.Offset] = value; }
            }

            #endregion

            private Exception FixedLengthException()
            {
                return new InvalidOperationException("The collection has a fixed size");
            }
            
        }
    }
}