using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity
{
    unsafe struct DynamicArray<T, TLabel> : IReadOnlyList<T>
        where T : unmanaged
    {
        public T* Ptr;
        public TLabel Label;
        public ulong Size;
        public ulong Capacity;

        readonly T IReadOnlyList<T>.this[int index] => Ptr[index];

        public readonly ref T this[int index] => ref Ptr[index];

        public readonly ref T this[ulong index] => ref Ptr[index];

        public readonly int Count => (int)Size;

        public readonly IEnumerator<T> GetEnumerator()
        {
            for (ulong i = 0; i < Size; i++)
                yield return this[i];
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
